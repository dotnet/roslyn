' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeClassTests
        Inherits AbstractCodeClassTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
class $$C {}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=10, absoluteOffset:=10, lineLength:=10)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=10)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=10)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=10)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint2()
            Dim code =
<Code>
class $$C { }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=10, absoluteOffset:=10, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint3()
            Dim code =
<Code>
class $$C {  }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=10, absoluteOffset:=10, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=7, absoluteOffset:=7, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=12)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint4()
            Dim code =
<Code>
using System;
[CLSCompliant(true)] class $$C { }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=36, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=28, absoluteOffset:=42, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=32)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint5()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { }
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=10, absoluteOffset:=45, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint6()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=46, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint7()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {

}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=46, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint8()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{

}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=46, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint9()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {void M() { }}
</Code>
            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=10, absoluteOffset:=45, lineLength:=22)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=22)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=22)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint10()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { void M() { } }
</Code>
            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=10, absoluteOffset:=45, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint11()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
    void M() { }
}
</Code>
            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=46, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint12()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{
    void M() { }
}
</Code>
            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=46, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=36, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=7, absoluteOffset:=42, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=15, lineLength:=20)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
class $$C {}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=10, absoluteOffset:=10, lineLength:=10)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=8, absoluteOffset:=8, lineLength:=10)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=10)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint2()
            Dim code =
<Code>
class $$C { }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=8, absoluteOffset:=8, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint3()
            Dim code =
<Code>
class $$C {  }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=8, absoluteOffset:=8, lineLength:=12)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=13, absoluteOffset:=13, lineLength:=12)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint4()
            Dim code =
<Code>
using System;
[CLSCompliant(true)] class $$C { }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=46, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=29, absoluteOffset:=43, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=47, lineLength:=32)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint5()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { }
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=11, absoluteOffset:=46, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=12, absoluteOffset:=47, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint6()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=46, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=4, lineOffset:=2, absoluteOffset:=47, lineLength:=1)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint7()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {

}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=47, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=2, absoluteOffset:=48, lineLength:=1)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint8()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{

}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=47, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=6, lineOffset:=2, absoluteOffset:=48, lineLength:=1)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint9()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {void M() { }}
</Code>
            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=57, lineLength:=22)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=22)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=23, absoluteOffset:=58, lineLength:=22)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint10()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { void M() { } }
</Code>
            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=24, absoluteOffset:=59, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=24)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=60, lineLength:=24)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint11()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
    void M() { }
}
</Code>
            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=1, absoluteOffset:=63, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=5, lineOffset:=2, absoluteOffset:=64, lineLength:=1)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint12()
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{
    void M() { }
}
</Code>
            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=35, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=63, lineLength:=1)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=8, absoluteOffset:=43, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=6, lineOffset:=2, absoluteOffset:=64, lineLength:=1)))
        End Sub

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
internal class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
public class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
class C { class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
class C { private class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
<Code>
class C { protected class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
class C { protected internal class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess8()
            Dim code =
<Code>
class C { internal class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess9()
            Dim code =
<Code>
class C { public class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

[Serializable]
class $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
class $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes4()
            Dim code =
<Code>using System;

[Serializable, CLSCompliant(true)]
class $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub
#End Region

#Region "Bases tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases2()
            Dim code =
<Code>
class $$C : object { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases3()
            Dim code =
<Code>
class C { }
class $$D : C { }
</Code>

            TestBases(code, IsElement("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases4()
            Dim code =
<Code>
interface I { }
class $$D : I { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases5()
            Dim code =
<Code>
class $$C : System.Collections.Generic.List&lt;int&gt; { }
</Code>

            TestBases(code, IsElement("List"))
        End Sub

#End Region

#Region "Children tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestChildren(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren2()
            Dim code =
<Code>
class $$C { void M() { } }
</Code>

            TestChildren(code, IsElement("M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren3()
            Dim code =
<Code>
[Obsolete]
class $$C { void M() { } }
</Code>

            TestChildren(code, IsElement("Obsolete"), IsElement("M"))
        End Sub


#End Region

#Region "ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClassKind_MainClass()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClassKind_PartialClass()
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Sub

#End Region

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestComment(code, String.Empty)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment2()
            Dim code =
<Code>
// Goo
// Bar
class $$C { }
</Code>

            TestComment(code, "Goo" & vbCrLf & "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment3()
            Dim code =
<Code>
class B { } // Goo
// Bar
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment4()
            Dim code =
<Code>
class B { } // Goo
/* Bar */
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment5()
            Dim code =
<Code>
class B { } // Goo
/*
    Bar
*/
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment6()
            Dim code =
<Code>
class B { } // Goo
/*
    Hello
    World!
*/
class $$C { }
</Code>

            TestComment(code, "Hello" & vbCrLf & "World!" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment7()
            Dim code =
<Code>
class B { } // Goo
/*
    Hello
    
    World!
*/
class $$C { }
</Code>

            TestComment(code, "Hello" & vbCrLf & vbCrLf & "World!" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment8()
            Dim code =
<Code>
/* This
 * is
 * a
 * multi-line
 * comment!
 */
class $$C { }
</Code>

            TestComment(code, "This" & vbCrLf & "is" & vbCrLf & "a" & vbCrLf & "multi-line" & vbCrLf & "comment!" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment9()
            Dim code =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
class $$C { }
</Code>

            TestComment(code, String.Empty)
        End Sub

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment1()
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class $$C { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>Hello World</summary>" & vbCrLf & "</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment2()
            Dim code =
<Code>
/// &lt;summary&gt;
/// Hello World
/// &lt;/summary&gt;
class $$C { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment3()
            Dim code =
<Code>
///    &lt;summary&gt;
/// Hello World
///&lt;/summary&gt;
class $$C { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "    <summary>" & vbCrLf & " Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment4()
            Dim code =
<Code>
/// &lt;summary&gt;
/// Summary
/// &lt;/summary&gt;
/// &lt;remarks&gt;Remarks&lt;/remarks&gt;
class $$C { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Summary" & vbCrLf & "</summary>" & vbCrLf & "<remarks>Remarks</remarks>" & vbCrLf & "</doc>")
        End Sub

#End Region

#Region "InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInheritanceKind_None()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInheritanceKind_Abstract()
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInheritanceKind_Sealed()
            Dim code =
<Code>
sealed class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInheritanceKind_New()
            Dim code =
<Code>
class C
{
    protected class Inner { }
}

class D
{
    new protected class $$Inner { }
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInheritanceKind_AbstractAndNew()
            Dim code =
<Code>
class C
{
    protected class Inner { }
}

class D
{
    new protected abstract class $$Inner { }
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

#End Region

#Region "IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsAbstract1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestIsAbstract(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsAbstract2()
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            TestIsAbstract(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsAbstract3()
            Dim code =
<Code>
abstract partial class $$C
{
}

partial class C
{
}
</Code>

            TestIsAbstract(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsAbstract4()
            Dim code =
<Code>
partial class $$C
{
}

abstract partial class C
{
}
</Code>

            TestIsAbstract(code, False)
        End Sub

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared2()
            Dim code =
<Code>
static class $$C
{
}
</Code>

            TestIsShared(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared3()
            Dim code =
<Code>
static partial class $$C
{
}

partial class C
{
}
</Code>

            TestIsShared(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared4()
            Dim code =
<Code>
partial class $$C
{
}

static partial class C
{
}
</Code>

            TestIsShared(code, False)
        End Sub

#End Region

#Region "IsDerivedFrom tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFromObject_Explicit()
            Dim code =
<Code>
class $$C : object { }
</Code>

            TestIsDerivedFrom(code, "System.Object", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_ObjectImplicit()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.Object", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_NotString()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.String", False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_NotNonexistent()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.ThisIsClearlyNotARealClassName", False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_UserClassInGlobalNamespace()
            Dim code =
<Code>
class B { }
class $$C : B { }
</Code>

            TestIsDerivedFrom(code, "B", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_UserClassInSameNamespace()
            Dim code =
<Code>
namespace NS
{
    class B { }
    class $$C : B { }
}
</Code>

            TestIsDerivedFrom(code, "NS.B", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDerivedFrom_UserClassInDifferentNamespace()
            Dim code =
<Code>
namespace NS1
{
    class B { }
}

namespace NS2
{
    class $$C : NS1.B { }
}
</Code>

            TestIsDerivedFrom(code, "NS1.B", True)
        End Sub

#End Region

#Region "Kind tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestKind1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementClass)
        End Sub
#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts2()
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParts3()
            Dim code =
<Code>
partial class $$C
{
}

partial class C
{
}
</Code>

            TestParts(code, 2)
        End Sub
#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

class $$C { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable()]
class C { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

[Serializable]
class $$C { }
</Code>

            Dim expected =
<Code>
using System;

[Serializable]
[CLSCompliant(true)]
class C { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment1() As Task
            Dim code =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
class $$C { }
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[CLSCompliant(true)]
class C { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment2() As Task
            Dim code =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[Serializable]
class $$C { }
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[CLSCompliant(true)]
[Serializable]
class C { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment3() As Task
            Dim code =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[Serializable]
class $$C { }
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[Serializable]
[CLSCompliant(true)]
class C { }
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C : B { }
</Code>
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase2() As Task
            Dim code =
<Code>
class $$C : B { }
</Code>

            Dim expected =
<Code>
class C : A, B { }
</Code>
            Await TestAddBase(code, "A", Nothing, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddBase3() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C : B
{
}
</Code>
            Await TestAddBase(code, "B", Nothing, expected)
        End Function

#End Region

#Region "AddEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEvent1() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler E;
}
</Code>

            Await TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEvent2() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler E
    {
        add
        {
        }

        remove
        {
        }
    }
}
</Code>

            Await TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler", .CreatePropertyStyleEvent = True})
        End Function

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction2() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction3() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "System.Void"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction4() As Task
            Dim code =
<Code>
class $$C
{
    int i;
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo()
    {

    }

    int i;
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction5() As Task
            Dim code =
<Code>
class $$C
{
    int i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;

    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void", .Position = 1})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction6() As Task
            Dim code =
<Code>
class $$C
{
    int i;
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;

    void Goo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Goo", .Type = "void", .Position = "i"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Constructor1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    C()
    {
    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Constructor2() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    public C()
    {
    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor, .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_EscapedName() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    public void @as()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "@as", .Type = "void", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_Destructor() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    ~C()
    {
    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionDestructor, .Type = "void", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Function

        <WorkItem(1172038, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1172038")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddFunction_AfterIncompleteMember() As Task
            Dim code =
<Code>
class $$C
{
    private void M1()
    private void
}
</Code>

            Dim expected =
<Code>
class C
{
    private void M1()
    private void private void M2()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "M2", .Type = "void", .Position = -1, .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Function

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddImplementedInterface1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface2() As Task
            Dim code =
<Code>
class $$C { }
interface I { }
</Code>

            Dim expected =
<Code>
class C : I { }
interface I { }
</Code>

            Await TestAddImplementedInterface(code, "I", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface3() As Task
            Dim code =
<Code>
class $$C : I { }
interface I { }
interface J { }
</Code>

            Dim expected =
<Code>
class C : I, J { }
interface I { }
interface J { }
</Code>

            Await TestAddImplementedInterface(code, "J", -1, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface4() As Task
            Dim code =
<Code>
class $$C : I { }
interface I { }
interface J { }
</Code>

            Dim expected =
<Code>
class C : J, I { }
interface I { }
interface J { }
</Code>

            Await TestAddImplementedInterface(code, "J", 0, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImplementedInterface5() As Task
            Dim code =
<Code>
class $$C : I, K { }
interface I { }
interface J { }
interface K { }
</Code>

            Dim expected =
<Code>
class C : I, J, K { }
interface I { }
interface J { }
interface K { }
</Code>

            Await TestAddImplementedInterface(code, "J", 1, expected)
        End Function

#End Region

#Region "AddProperty tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty1() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    string Name
    {
        get => default(string);
        set
        {
        }
    }
}
</Code>

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty_NoCodeStyle1() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    string Name
    {
        get
        {
            return default(string);
        }

        set
        {
        }
    }
}
</Code>

            Await TestAddProperty(
                code, expected,
                New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString},
                New Dictionary(Of OptionKey, Object) From {
                    {CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement},
                    {CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement}
                })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty2() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    string Name => default(string);
}
</Code>

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty_NoCodeStyle2() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    string Name
    {
        get
        {
            return default(string);
        }
    }
}
</Code>

            Await TestAddProperty(
                code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString},
                New Dictionary(Of OptionKey, Object) From {
                    {CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement},
                    {CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement}
                })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddProperty3() As Task
            Dim code =
<Code>
class C$$
{
}
</Code>

            Dim expected =
<Code>
class C
{
    string Name
    {
        set
        {
        }
    }
}
</Code>

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = Nothing, .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Function

#End Region

#Region "AddVariable tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable2() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable3() As Task
            Dim code =
<Code>
class $$C
{
    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo() { }

    int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Goo"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable4() As Task
            Dim code =
<Code>
class $$C
{
    int x;
    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
    int i;

    void Goo() { }
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable5() As Task
            Dim code =
<Code>
class $$C
{
    int x;

    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
    int i;

    void Goo() { }
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable6() As Task
            Dim code =
<Code>
class $$C
{
    int x, y;

    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;

    void Goo() { }
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable7() As Task
            Dim code =
<Code>
class $$C
{
    int x, y;

    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;

    void Goo() { }
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable8() As Task
            Dim code =
<Code>
class $$C
{
    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;

    void Goo() { }
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = 0})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable9() As Task
            Dim code =
<Code>
class $$C
{
    void Goo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Goo() { }

    int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = -1})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable10() As Task
            Dim code =
<Code>
class $$C
{
    int x;
    int y;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
    int i;
    int y;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable11() As Task
            Dim code =
<Code>
class $$C
{
    int x, y;
    int z;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;
    int z;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable12() As Task
            Dim code =
<Code>
class $$C
{
    int x, y;
    int z;
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;
    int z;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable13() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    public int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Function

        <WorkItem(545238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545238")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable14() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    private int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Function

        <WorkItem(546556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546556")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable15() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    internal int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProject})
        End Function

        <WorkItem(546556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546556")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable16() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    protected internal int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected})
        End Function

        <WorkItem(546556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546556")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable17() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
    protected int i;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProtected})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariableOutsideOfRegion() As Task
            Dim code =
<Code>
class $$C
{
    #region Goo
    int i = 0;
    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Goo
    int i = 0;
    #endregion

    int j;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Function

        <WorkItem(529865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529865")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariableAfterComment() As Task
            Dim code =
<Code>
class $$C
{
    int i = 0; // Goo
}
</Code>

            Dim expected =
<Code>
class C
{
    int i = 0; // Goo
    int j;
}
</Code>

            Await TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Function

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase1() As Task
            Dim code =
<Code>
class $$C : B { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>
            Await TestRemoveBase(code, "B", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveBase2() As Task
            Dim code =
<Code>
class $$C : A, B { }
</Code>

            Dim expected =
<Code>
class C : B { }
</Code>
            Await TestRemoveBase(code, "A", expected)
        End Function

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface1() As Task
            Dim code =
<Code>
class $$C : I { }
interface I { }
</Code>

            Dim expected =
<Code>
class C { }
interface I { }
</Code>
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveImplementedInterface2() As Task
            Dim code =
<Code>
class $$C : A, I { }
class A { }
interface I { }
</Code>

            Dim expected =
<Code>
class C : A { }
class A { }
interface I { }
</Code>
            Await TestRemoveImplementedInterface(code, "I", expected)
        End Function

#End Region

#Region "RemoveMember tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember1() As Task
            Dim code =
<Code>
class $$C
{
    void Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember2() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    /// <summary>
    /// Doc comment.
    /// </summary>
    void Goo()
    {
    }
}
]]></Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember3() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    // Comment comment comment
    void Goo()
    {
    }
}
]]></Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember4() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    // Comment comment comment

    void Goo()
    {
    }
}
]]></Code>

            Dim expected =
<Code>
class C
{
    // Comment comment comment
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember5() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    #region A region
    int a;
    #endregion
    /// <summary>
    /// Doc comment.
    /// </summary>
    void Goo()
    {
    }
}
]]></Code>

            Dim expected =
<Code>
class C
{
    #region A region
    int a;
    #endregion
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember6() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    // This comment remains.

    // This comment is deleted.
    /// <summary>
    /// This comment is deleted.
    /// </summary>
    void Goo()
    {
    }
}
]]></Code>

            Dim expected =
<Code>
class C
{
    // This comment remains.
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember7() As Task
            Dim code =
<Code><![CDATA[
class $$C
{
    int a;
    int b;
    int d;
}
]]></Code>

            Dim expected =
<Code>
class C
{
    int a;
    int d;
}
</Code>

            Await TestRemoveChild(code, expected, "b")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember8() As Task
            Dim code =
<Code>
class $$C
{
    void Alpha()
    {
    }

    void Goo()
    {
    }

    void Beta()
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Alpha()
    {
    }

    void Beta()
    {
    }
}
</Code>

            Await TestRemoveChild(code, expected, "Goo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember_Event1() As Task
            Dim code =
<Code>
class $$C
{
    event System.EventHandler E;
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestRemoveChild(code, expected, "E")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember_Event2() As Task
            Dim code =
<Code>
class $$C
{
    event System.EventHandler E, F, G;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler F, G;
}
</Code>

            Await TestRemoveChild(code, expected, "E")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember_Event3() As Task
            Dim code =
<Code>
class $$C
{
    event System.EventHandler E, F, G;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler E, G;
}
</Code>

            Await TestRemoveChild(code, expected, "F")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember_Event4() As Task
            Dim code =
<Code>
class $$C
{
    event System.EventHandler E, F, G;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler E, F;
}
</Code>

            Await TestRemoveChild(code, expected, "G")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember_Event5() As Task
            Dim code =
<Code>
class $$C
{
    event System.EventHandler E
    {
        add { }
        remove { }
    }
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestRemoveChild(code, expected, "E")
        End Function

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
public class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
public class $$C
{
}
</Code>

            Dim expected =
<Code>
internal class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
protected internal class $$C
{
}
</Code>

            Dim expected =
<Code>
public class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
public class $$C
{
}
</Code>

            Dim expected =
<Code>
public class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
public class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
public class $$C
{
}
</Code>

            Dim expected =
<Code>
public class C
{
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
class C
{
    class $$D
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    private class D
    {
    }
}
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

#End Region

#Region "Set ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind2() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
partial class C
{
}
</Code>

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetClassKind3() As Task
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment1() As Task
            Dim code =
<Code>
// Goo

// Bar
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            Await TestSetComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment2() As Task
            Dim code =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
class $$C { }
</Code>

            Dim expected =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
// Bar
class C { }
</Code>

            Await TestSetComment(code, expected, "Bar")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
            Dim code =
<Code>
// Goo

// Bar
class $$C { }
</Code>

            Dim expected =
<Code>
// Blah
class C { }
</Code>

            Await TestSetComment(code, expected, "Blah")
        End Function

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            Await TestSetDocComment(code, expected, Nothing, ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</doc>", ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml2() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>", ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment2() As Task
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment3() As Task
            Dim code =
<Code>
// Goo
class $$C { }
</Code>

            Dim expected =
<Code>
// Goo
/// &lt;summary&gt;Blah&lt;/summary&gt;
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment4() As Task
            Dim code =
<Code>
/// &lt;summary&gt;FogBar&lt;/summary&gt;
// Goo
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
// Goo
class C { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment5() As Task
            Dim code =
<Code>
namespace N
{
    class $$C { }
}
</Code>

            Dim expected =
<Code>
namespace N
{
    /// &lt;summary&gt;Hello World&lt;/summary&gt;
    class C { }
}
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Function

#End Region

#Region "Set InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
abstract class C
{
}
</Code>

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind2() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
sealed class C
{
}
</Code>

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind3() As Task
            Dim code =
<Code>
class C
{
    class $$D
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    abstract class D
    {
    }
}
</Code>

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInheritanceKind4() As Task
            Dim code =
<Code>
class C
{
    class $$D
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    new sealed class D
    {
    }
}
</Code>

            Await TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

#End Region

#Region "Set IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
abstract class C
{
}
</Code>

            Await TestSetIsAbstract(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract2() As Task
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestSetIsAbstract(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract3() As Task
            Dim code =
<Code>
class C
{
    new class $$D
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    abstract new class D
    {
    }
}
</Code>

            Await TestSetIsAbstract(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract4() As Task
            Dim code =
<Code>
class C
{
    abstract new class $$D
    {
    }
}
</Code>

            Dim expected =
<Code>
class C
{
    new class D
    {
    }
}
</Code>

            Await TestSetIsAbstract(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsAbstract5() As Task
            ' Note: In Dev11 the C# Code Model will happily include an abstract modifier
            ' on a sealed class. This differs from VB Code Model where the NotInheritable
            ' modifier will be removed when adding MustInherit. In Roslyn, we take the Dev11
            ' VB behavior for both C# and VB since it produces more correct code.

            Dim code =
<Code>
sealed class $$C
{
}
</Code>

            Dim expected =
<Code>
abstract class C
{
}
</Code>

            Await TestSetIsAbstract(code, expected, True)
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
static class C
{
}
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
static class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
class $$Goo
{
}
</Code>

            Dim expected =
<Code>
class Bar
{
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
            Dim code =
<Code>
class $$Goo
{
    Goo()
    {
    }
}
</Code>

            Dim expected =
<Code>
class Bar
{
    Bar()
    {
    }
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName3() As Task
            Dim code =
<Code>
partial class $$Goo
{
}

partial class Goo
{
}
</Code>

            Dim expected =
<Code>
partial class Bar
{
}

partial class Goo
{
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetBaseName1()
            Dim code =
<Code>
using N.M;

namespace N
{
    namespace M
    {
        class Generic&lt;T&gt; { }
    }
}

class $$C : Generic&lt;string&gt;
{
}
</Code>

            TestGetBaseName(code, "N.M.Generic<string>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddDeleteManyTimes()
            Dim code =
<Code>
class C$$
{
}
</Code>

            TestElement(code,
                Sub(codeClass)
                    For i = 1 To 100
                        Dim variable = codeClass.AddVariable("x", "System.Int32")
                        codeClass.RemoveMember(variable)
                    Next
                End Sub)
        End Sub

        <WorkItem(8423, "https://github.com/dotnet/roslyn/issues/8423")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddAndRemoveViaTextChangeManyTimes()
            Dim code =
<Code>
class C$$
{
}
</Code>

            TestElement(code,
                Sub(state, codeClass)
                    For i = 1 To 100
                        Dim variable = codeClass.AddVariable("x", "System.Int32")

                        ' Now, delete the variable that we just added.
                        Dim startPoint = variable.StartPoint
                        Dim document = state.FileCodeModelObject.GetDocument()
                        Dim text = document.GetTextAsync(CancellationToken.None).Result
                        Dim textLine = text.Lines(startPoint.Line - 1)
                        text = text.Replace(textLine.SpanIncludingLineBreak, "")
                        document = document.WithText(text)

                        Dim result = state.VisualStudioWorkspace.TryApplyChanges(document.Project.Solution)
                        Assert.True(result, "Attempt to apply changes to workspace failed.")
                    Next
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeClass2)(code)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExternalClass_ImplementedInterfaces()
            Dim code =
<Code>
class $$Goo : System.Collections.Generic.List&lt;int&gt;
{
}
</Code>

            TestElement(code,
                Sub(codeClass)
                    Dim listType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(listType)

                    Assert.Equal(8, listType.ImplementedInterfaces.Count)
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExternalFunction_Overloads()
            Dim code =
<Code>
class $$Derived : System.Console
{
}
</Code>
            TestElement(
                code,
                Sub(codeClass)
                    Dim baseType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(baseType)

                    Dim method1 = TryCast(baseType.Members.Item("WriteLine"), EnvDTE80.CodeFunction2)
                    Assert.NotNull(method1)

                    Assert.Equal(True, method1.IsOverloaded)
                    Assert.Equal(19, method1.Overloads.Count)
                End Sub)

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExternalFunction_Overloads_NotOverloaded()
            Dim code =
<Code>
class $$Derived : System.Console
{
}
</Code>
            TestElement(
                code,
                Sub(codeClass)
                    Dim baseType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(baseType)

                    Dim method2 = TryCast(baseType.Members.Item("Clear"), EnvDTE80.CodeFunction2)
                    Assert.NotNull(method2)

                    Assert.Equal(1, method2.Overloads.Count)
                    Assert.Equal("System.Console.Clear", TryCast(method2.Overloads.Item(1), EnvDTE80.CodeFunction2).FullName)
                End Sub)

        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
