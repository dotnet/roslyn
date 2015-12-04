' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeClassTests
        Inherits AbstractCodeClassTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint1()
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
        Public Sub GetStartPoint2()
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
        Public Sub GetStartPoint3()
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
        Public Sub GetStartPoint4()
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
        Public Sub GetStartPoint5()
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
        Public Sub GetStartPoint6()
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
        Public Sub GetStartPoint7()
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
        Public Sub GetStartPoint8()
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
        Public Sub GetStartPoint9()
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
        Public Sub GetStartPoint10()
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
        Public Sub GetStartPoint11()
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
        Public Sub GetStartPoint12()
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
        Public Sub GetEndPoint1()
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
        Public Sub GetEndPoint2()
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
        Public Sub GetEndPoint3()
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
        Public Sub GetEndPoint4()
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
        Public Sub GetEndPoint5()
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
        Public Sub GetEndPoint6()
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
        Public Sub GetEndPoint7()
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
        Public Sub GetEndPoint8()
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
        Public Sub GetEndPoint9()
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
        Public Sub GetEndPoint10()
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
        Public Sub GetEndPoint11()
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
        Public Sub GetEndPoint12()
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
        Public Sub Access1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
internal class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
public class $$C { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
class C { class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
class C { private class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
class C { protected class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access7()
            Dim code =
<Code>
class C { protected internal class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access8()
            Dim code =
<Code>
class C { internal class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access9()
            Dim code =
<Code>
class C { public class $$D { } }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes2()
            Dim code =
<Code>
using System;

[Serializable]
class $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes3()
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
class $$C { }
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Attributes4()
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
        Public Sub Bases1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Bases2()
            Dim code =
<Code>
class $$C : object { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Bases3()
            Dim code =
<Code>
class C { }
class $$D : C { }
</Code>

            TestBases(code, IsElement("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Bases4()
            Dim code =
<Code>
interface I { }
class $$D : I { }
</Code>

            TestBases(code, IsElement("Object"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Bases5()
            Dim code =
<Code>
class $$C : System.Collections.Generic.List&lt;int&gt; { }
</Code>

            TestBases(code, IsElement("List"))
        End Sub

#End Region

#Region "Children tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Children1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestChildren(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Children2()
            Dim code =
<Code>
class $$C { void M() { } }
</Code>

            TestChildren(code, IsElement("M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Children3()
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
        Public Sub ClassKind_MainClass()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassKind_PartialClass()
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
        Public Sub Comment1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestComment(code, String.Empty)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment2()
            Dim code =
<Code>
// Foo
// Bar
class $$C { }
</Code>

            TestComment(code, "Foo" & vbCrLf & "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment3()
            Dim code =
<Code>
class B { } // Foo
// Bar
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment4()
            Dim code =
<Code>
class B { } // Foo
/* Bar */
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment5()
            Dim code =
<Code>
class B { } // Foo
/*
    Bar
*/
class $$C { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment6()
            Dim code =
<Code>
class B { } // Foo
/*
    Hello
    World!
*/
class $$C { }
</Code>

            TestComment(code, "Hello" & vbCrLf & "World!" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment7()
            Dim code =
<Code>
class B { } // Foo
/*
    Hello
    
    World!
*/
class $$C { }
</Code>

            TestComment(code, "Hello" & vbCrLf & vbCrLf & "World!" & vbCrLf)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment8()
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
        Public Sub Comment9()
            Dim code =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
class $$C { }
</Code>

            TestComment(code, String.Empty)
        End Sub

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment1()
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class $$C { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>Hello World</summary>" & vbCrLf & "</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DocComment2()
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
        Public Sub DocComment3()
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
        Public Sub DocComment4()
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
        Public Sub InheritanceKind_None()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_Abstract()
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_Sealed()
            Dim code =
<Code>
sealed class $$C
{
}
</Code>

            TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InheritanceKind_New()
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
        Public Sub InheritanceKind_AbstractAndNew()
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
        Public Sub IsAbstract1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestIsAbstract(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract2()
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            TestIsAbstract(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsAbstract3()
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
        Public Sub IsAbstract4()
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
        Public Sub IsShared1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
            Dim code =
<Code>
static class $$C
{
}
</Code>

            TestIsShared(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared3()
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
        Public Sub IsShared4()
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
        Public Sub IsDerivedFromObject_Explicit()
            Dim code =
<Code>
class $$C : object { }
</Code>

            TestIsDerivedFrom(code, "System.Object", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDerivedFrom_ObjectImplicit()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.Object", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDerivedFrom_NotString()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.String", False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDerivedFrom_NotNonexistent()
            Dim code =
<Code>
class $$C { }
</Code>

            TestIsDerivedFrom(code, "System.ThisIsClearlyNotARealClassName", False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDerivedFrom_UserClassInGlobalNamespace()
            Dim code =
<Code>
class B { }
class $$C : B { }
</Code>

            TestIsDerivedFrom(code, "B", True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsDerivedFrom_UserClassInSameNamespace()
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
        Public Sub IsDerivedFrom_UserClassInDifferentNamespace()
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
        Public Sub Kind()
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
        Public Sub Parts1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts2()
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            TestParts(code, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Parts3()
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
        Public Sub AddAttribute1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment1()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment2()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment3()
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
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

#End Region

#Region "AddBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase1()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C : B { }
</Code>
            TestAddBase(code, "B", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase2()
            Dim code =
<Code>
class $$C : B { }
</Code>

            Dim expected =
<Code>
class C : A, B { }
</Code>
            TestAddBase(code, "A", Nothing, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddBase3()
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
            TestAddBase(code, "B", Nothing, expected)
        End Sub

#End Region

#Region "AddEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEvent1()
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

            TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEvent2()
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

            TestAddEvent(code, expected, New EventData With {.Name = "E", .FullDelegateName = "System.EventHandler", .CreatePropertyStyleEvent = True})
        End Sub

#End Region

#Region "AddFunction tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction1()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C
{
    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction2()
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
    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction3()
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
    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "System.Void"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction4()
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
    void Foo()
    {

    }

    int i;
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction5()
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

    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void", .Position = 1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction6()
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

    void Foo()
    {

    }
}
</Code>

            TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void", .Position = "i"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Constructor1()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Constructor2()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionConstructor, .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_EscapedName()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "@as", .Type = "void", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_Destructor()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "C", .Kind = EnvDTE.vsCMFunction.vsCMFunctionDestructor, .Type = "void", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Sub

        <WorkItem(1172038)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddFunction_AfterIncompleteMember()
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

            TestAddFunction(code, expected, New FunctionData With {.Name = "M2", .Type = "void", .Position = -1, .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Sub

#End Region

#Region "AddImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface1()
            Dim code =
<Code>
class $$C { }
</Code>

            TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface2()
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

            TestAddImplementedInterface(code, "I", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface3()
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

            TestAddImplementedInterface(code, "J", -1, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface4()
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

            TestAddImplementedInterface(code, "J", 0, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImplementedInterface5()
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

            TestAddImplementedInterface(code, "J", 1, expected)
        End Sub

#End Region

#Region "AddProperty tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty1()
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

            TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty2()
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

            TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddProperty3()
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

            TestAddProperty(code, expected, New PropertyData With {.GetterName = Nothing, .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
        End Sub

#End Region

#Region "AddVariable tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable1()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable2()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable3()
            Dim code =
<Code>
class $$C
{
    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo() { }

    int i;
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable4()
            Dim code =
<Code>
class $$C
{
    int x;
    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
    int i;

    void Foo() { }
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable5()
            Dim code =
<Code>
class $$C
{
    int x;

    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x;
    int i;

    void Foo() { }
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable6()
            Dim code =
<Code>
class $$C
{
    int x, y;

    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;

    void Foo() { }
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable7()
            Dim code =
<Code>
class $$C
{
    int x, y;

    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int x, y;
    int i;

    void Foo() { }
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable8()
            Dim code =
<Code>
class $$C
{
    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    int i;

    void Foo() { }
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = 0})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable9()
            Dim code =
<Code>
class $$C
{
    void Foo() { }
}
</Code>

            Dim expected =
<Code>
class C
{
    void Foo() { }

    int i;
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = -1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable10()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable11()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable12()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable13()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPublic})
        End Sub

        <WorkItem(545238)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable14()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessPrivate})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable15()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProject})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable16()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected})
        End Sub

        <WorkItem(546556)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariable17()
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

            TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Access = EnvDTE.vsCMAccess.vsCMAccessProtected})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariableOutsideOfRegion()
            Dim code =
<Code>
class $$C
{
    #region Foo
    int i = 0;
    #endregion
}
</Code>

            Dim expected =
<Code>
class C
{
    #region Foo
    int i = 0;
    #endregion

    int j;
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Sub

        <WorkItem(529865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddVariableAfterComment()
            Dim code =
<Code>
class $$C
{
    int i = 0; // Foo
}
</Code>

            Dim expected =
<Code>
class C
{
    int i = 0; // Foo
    int j;
}
</Code>

            TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Sub

#End Region

#Region "RemoveBase tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase1()
            Dim code =
<Code>
class $$C : B { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>
            TestRemoveBase(code, "B", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveBase2()
            Dim code =
<Code>
class $$C : A, B { }
</Code>

            Dim expected =
<Code>
class C : B { }
</Code>
            TestRemoveBase(code, "A", expected)
        End Sub

#End Region

#Region "RemoveImplementedInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface1()
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
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveImplementedInterface2()
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
            TestRemoveImplementedInterface(code, "I", expected)
        End Sub

#End Region

#Region "RemoveMember tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember1()
            Dim code =
<Code>
class $$C
{
    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember2()
            Dim code =
<Code><![CDATA[
class $$C
{
    /// <summary>
    /// Doc comment.
    /// </summary>
    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember3()
            Dim code =
<Code><![CDATA[
class $$C
{
    // Comment comment comment
    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember4()
            Dim code =
<Code><![CDATA[
class $$C
{
    // Comment comment comment

    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember5()
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
    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember6()
            Dim code =
<Code><![CDATA[
class $$C
{
    // This comment remains.

    // This comment is deleted.
    /// <summary>
    /// This comment is deleted.
    /// </summary>
    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember7()
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

            TestRemoveChild(code, expected, "b")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember8()
            Dim code =
<Code>
class $$C
{
    void Alpha()
    {
    }

    void Foo()
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

            TestRemoveChild(code, expected, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember_Event1()
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

            TestRemoveChild(code, expected, "E")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember_Event2()
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

            TestRemoveChild(code, expected, "E")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember_Event3()
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

            TestRemoveChild(code, expected, "F")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember_Event4()
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

            TestRemoveChild(code, expected, "G")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RemoveMember_Event5()
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

            TestRemoveChild(code, expected, "E")
        End Sub

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

#End Region

#Region "Set ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind1()
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

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind2()
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

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetClassKind3()
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

            TestSetClassKind(code, expected, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Sub

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment1()
            Dim code =
<Code>
// Foo

// Bar
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            TestSetComment(code, expected, Nothing)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment2()
            Dim code =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
class $$C { }
</Code>

            Dim expected =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
// Bar
class C { }
</Code>

            TestSetComment(code, expected, "Bar")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetComment3()
            Dim code =
<Code>
// Foo

// Bar
class $$C { }
</Code>

            Dim expected =
<Code>
// Blah
class C { }
</Code>

            TestSetComment(code, expected, "Blah")
        End Sub

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_Nothing()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            TestSetDocComment(code, expected, Nothing, ThrowsArgumentException(Of String))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_InvalidXml1()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            TestSetDocComment(code, expected, "<doc><summary>Blah</doc>", ThrowsArgumentException(Of String))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment_InvalidXml2()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }
</Code>

            TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>", ThrowsArgumentException(Of String))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment1()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class C { }
</Code>

            TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment2()
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

            TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment3()
            Dim code =
<Code>
// Foo
class $$C { }
</Code>

            Dim expected =
<Code>
// Foo
/// &lt;summary&gt;Blah&lt;/summary&gt;
class C { }
</Code>

            TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment4()
            Dim code =
<Code>
/// &lt;summary&gt;FogBar&lt;/summary&gt;
// Foo
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
// Foo
class C { }
</Code>

            TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetDocComment5()
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

            TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Sub

#End Region

#Region "Set InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind1()
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

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind2()
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

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind3()
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

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInheritanceKind4()
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

            TestSetInheritanceKind(code, expected, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Sub

#End Region

#Region "Set IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract1()
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

            TestSetIsAbstract(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract2()
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

            TestSetIsAbstract(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract3()
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

            TestSetIsAbstract(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract4()
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

            TestSetIsAbstract(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsAbstract5()
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

            TestSetIsAbstract(code, expected, True)
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
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

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
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

            TestSetIsShared(code, expected, False)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
class $$Foo
{
}
</Code>

            Dim expected =
<Code>
class Bar
{
}
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName2()
            Dim code =
<Code>
class $$Foo
{
    Foo()
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

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName3()
            Dim code =
<Code>
partial class $$Foo
{
}

partial class Foo
{
}
</Code>

            Dim expected =
<Code>
partial class Bar
{
}

partial class Foo
{
}
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetBaseName1()
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

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Namespace", "Bases", "Members", "Access", "Attributes",
                 "DocComment", "Comment", "DerivedTypes", "ImplementedInterfaces", "IsAbstract",
                 "ClassKind", "PartialClasses", "DataTypeKind", "Parts", "InheritanceKind", "IsGeneric",
                 "IsShared"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ExternalClass_ImplementedInterfaces()
            Dim code =
<Code>
class $$Foo : System.Collections.Generic.List&lt;int&gt;
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
        Public Sub ExternalFunction_Overloads()
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
        Public Sub ExternalFunction_Overloads_NotOverloaded()
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
