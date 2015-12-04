' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeClassTests
        Inherits AbstractCodeClassTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
class $$C {}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint2() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint3() As Task
            Dim code =
<Code>
class $$C {  }
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint4() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)] class $$C { }
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint5() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { }
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint6() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint7() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {

}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint8() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{

}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint9() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {void M() { }}
</Code>
            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint10() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { void M() { } }
</Code>
            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint11() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
    void M() { }
}
</Code>
            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint12() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{
    void M() { }
}
</Code>
            Await TestGetStartPoint(code,
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
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
class $$C {}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint2() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint3() As Task
            Dim code =
<Code>
class $$C {  }
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint4() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)] class $$C { }
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint5() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { }
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint6() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint7() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {

}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint8() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{

}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint9() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {void M() { }}
</Code>
            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint10() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C { void M() { } }
</Code>
            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint11() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C {
    void M() { }
}
</Code>
            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint12() As Task
            Dim code =
<Code>
using System;
[CLSCompliant(true)]
class $$C
{
    void M() { }
}
</Code>
            Await TestGetEndPoint(code,
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
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
internal class $$C { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
public class $$C { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
class C { class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
class C { private class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
class C { protected class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
class C { protected internal class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess8() As Task
            Dim code =
<Code>
class C { internal class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess9() As Task
            Dim code =
<Code>
class C { public class $$D { } }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestAttributes(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes2() As Task
            Dim code =
<Code>
using System;

[Serializable]
class $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes3() As Task
            Dim code =
<Code>using System;

[Serializable]
[CLSCompliant(true)]
class $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes4() As Task
            Dim code =
<Code>using System;

[Serializable, CLSCompliant(true)]
class $$C { }
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("CLSCompliant"))
        End Function
#End Region

#Region "Bases tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestBases(code, IsElement("Object"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases2() As Task
            Dim code =
<Code>
class $$C : object { }
</Code>

            Await TestBases(code, IsElement("Object"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases3() As Task
            Dim code =
<Code>
class C { }
class $$D : C { }
</Code>

            Await TestBases(code, IsElement("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases4() As Task
            Dim code =
<Code>
interface I { }
class $$D : I { }
</Code>

            Await TestBases(code, IsElement("Object"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases5() As Task
            Dim code =
<Code>
class $$C : System.Collections.Generic.List&lt;int&gt; { }
</Code>

            Await TestBases(code, IsElement("List"))
        End Function

#End Region

#Region "Children tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestChildren1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestChildren(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestChildren2() As Task
            Dim code =
<Code>
class $$C { void M() { } }
</Code>

            Await TestChildren(code, IsElement("M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestChildren3() As Task
            Dim code =
<Code>
[Obsolete]
class $$C { void M() { } }
</Code>

            Await TestChildren(code, IsElement("Obsolete"), IsElement("M"))
        End Function


#End Region

#Region "ClassKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_MainClass() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassKind_PartialClass() As Task
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            Await TestClassKind(code, EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
        End Function

#End Region

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestComment(code, String.Empty)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment2() As Task
            Dim code =
<Code>
// Foo
// Bar
class $$C { }
</Code>

            Await TestComment(code, "Foo" & vbCrLf & "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment3() As Task
            Dim code =
<Code>
class B { } // Foo
// Bar
class $$C { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment4() As Task
            Dim code =
<Code>
class B { } // Foo
/* Bar */
class $$C { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment5() As Task
            Dim code =
<Code>
class B { } // Foo
/*
    Bar
*/
class $$C { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment6() As Task
            Dim code =
<Code>
class B { } // Foo
/*
    Hello
    World!
*/
class $$C { }
</Code>

            Await TestComment(code, "Hello" & vbCrLf & "World!" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment7() As Task
            Dim code =
<Code>
class B { } // Foo
/*
    Hello
    
    World!
*/
class $$C { }
</Code>

            Await TestComment(code, "Hello" & vbCrLf & vbCrLf & "World!" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment8() As Task
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

            Await TestComment(code, "This" & vbCrLf & "is" & vbCrLf & "a" & vbCrLf & "multi-line" & vbCrLf & "comment!" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment9() As Task
            Dim code =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
class $$C { }
</Code>

            Await TestComment(code, String.Empty)
        End Function

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment1() As Task
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
class $$C { }
</Code>

            Await TestDocComment(code, "<doc>" & vbCrLf & "<summary>Hello World</summary>" & vbCrLf & "</doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment2() As Task
            Dim code =
<Code>
/// &lt;summary&gt;
/// Hello World
/// &lt;/summary&gt;
class $$C { }
</Code>

            Await TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment3() As Task
            Dim code =
<Code>
///    &lt;summary&gt;
/// Hello World
///&lt;/summary&gt;
class $$C { }
</Code>

            Await TestDocComment(code, "<doc>" & vbCrLf & "    <summary>" & vbCrLf & " Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDocComment4() As Task
            Dim code =
<Code>
/// &lt;summary&gt;
/// Summary
/// &lt;/summary&gt;
/// &lt;remarks&gt;Remarks&lt;/remarks&gt;
class $$C { }
</Code>

            Await TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Summary" & vbCrLf & "</summary>" & vbCrLf & "<remarks>Remarks</remarks>" & vbCrLf & "</doc>")
        End Function

#End Region

#Region "InheritanceKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_None() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_Abstract() As Task
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_Sealed() As Task
            Dim code =
<Code>
sealed class $$C
{
}
</Code>

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_New() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestInheritanceKind_AbstractAndNew() As Task
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

            Await TestInheritanceKind(code, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew)
        End Function

#End Region

#Region "IsAbstract tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestIsAbstract(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract2() As Task
            Dim code =
<Code>
abstract class $$C
{
}
</Code>

            Await TestIsAbstract(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract3() As Task
            Dim code =
<Code>
abstract partial class $$C
{
}

partial class C
{
}
</Code>

            Await TestIsAbstract(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsAbstract4() As Task
            Dim code =
<Code>
partial class $$C
{
}

abstract partial class C
{
}
</Code>

            Await TestIsAbstract(code, False)
        End Function

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestIsShared(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared2() As Task
            Dim code =
<Code>
static class $$C
{
}
</Code>

            Await TestIsShared(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared3() As Task
            Dim code =
<Code>
static partial class $$C
{
}

partial class C
{
}
</Code>

            Await TestIsShared(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared4() As Task
            Dim code =
<Code>
partial class $$C
{
}

static partial class C
{
}
</Code>

            Await TestIsShared(code, False)
        End Function

#End Region

#Region "IsDerivedFrom tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFromObject_Explicit() As Task
            Dim code =
<Code>
class $$C : object { }
</Code>

            Await TestIsDerivedFrom(code, "System.Object", True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_ObjectImplicit() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestIsDerivedFrom(code, "System.Object", True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_NotString() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestIsDerivedFrom(code, "System.String", False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_NotNonexistent() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestIsDerivedFrom(code, "System.ThisIsClearlyNotARealClassName", False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_UserClassInGlobalNamespace() As Task
            Dim code =
<Code>
class B { }
class $$C : B { }
</Code>

            Await TestIsDerivedFrom(code, "B", True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_UserClassInSameNamespace() As Task
            Dim code =
<Code>
namespace NS
{
    class B { }
    class $$C : B { }
}
</Code>

            Await TestIsDerivedFrom(code, "NS.B", True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDerivedFrom_UserClassInDifferentNamespace() As Task
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

            Await TestIsDerivedFrom(code, "NS1.B", True)
        End Function

#End Region

#Region "Kind tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementClass)
        End Function
#End Region

#Region "Parts tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts2() As Task
            Dim code =
<Code>
partial class $$C
{
}
</Code>

            Await TestParts(code, 1)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParts3() As Task
            Dim code =
<Code>
partial class $$C
{
}

partial class C
{
}
</Code>

            Await TestParts(code, 2)
        End Function
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
    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
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
    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
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
    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "System.Void"})
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
    void Foo()
    {

    }

    int i;
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void"})
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

    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void", .Position = 1})
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

    void Foo()
    {

    }
}
</Code>

            Await TestAddFunction(code, expected, New FunctionData With {.Name = "Foo", .Type = "void", .Position = "i"})
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

        <WorkItem(1172038)>
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
        Public Async Function TestAddImplementedInterface1() As Task
            Dim code =
<Code>
class $$C { }
</Code>

            Await TestAddImplementedInterfaceThrows(Of ArgumentException)(code, "I", Nothing)
        End Function

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

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = "Name", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
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
    string Name
    {
        get
        {
            return default(string);
        }
    }
}
</Code>

            Await TestAddProperty(code, expected, New PropertyData With {.GetterName = "Name", .PutterName = Nothing, .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefString})
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "Foo"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable4() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable5() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable6() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "x"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable7() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = "y"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable8() As Task
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "i", .Type = "System.Int32", .Position = 0})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariable9() As Task
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

        <WorkItem(545238)>
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

        <WorkItem(546556)>
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

        <WorkItem(546556)>
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

        <WorkItem(546556)>
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

            Await TestAddVariable(code, expected, New VariableData With {.Name = "j", .Type = EnvDTE.vsCMTypeRef.vsCMTypeRefInt, .Position = "i"})
        End Function

        <WorkItem(529865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddVariableAfterComment() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember3() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember4() As Task
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

            Await TestRemoveChild(code, expected, "Foo")
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

            Await TestRemoveChild(code, expected, "Foo")
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

            Await TestRemoveChild(code, expected, "Foo")
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

            Await TestRemoveChild(code, expected, "Foo")
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
// Foo

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

            Await TestSetComment(code, expected, "Bar")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
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
// Foo
class $$C { }
</Code>

            Dim expected =
<Code>
// Foo
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
// Foo
class $$C { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
// Foo
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName3() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetBaseName1() As Task
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

            Await TestGetBaseName(code, "N.M.Generic<string>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
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

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExternalClass_ImplementedInterfaces() As Task
            Dim code =
<Code>
class $$Foo : System.Collections.Generic.List&lt;int&gt;
{
}
</Code>

            Await TestElement(code,
                Sub(codeClass)
                    Dim listType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(listType)

                    Assert.Equal(8, listType.ImplementedInterfaces.Count)
                End Sub)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExternalFunction_Overloads() As Task
            Dim code =
<Code>
class $$Derived : System.Console
{
}
</Code>
            Await TestElement(
                code,
                Sub(codeClass)
                    Dim baseType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(baseType)

                    Dim method1 = TryCast(baseType.Members.Item("WriteLine"), EnvDTE80.CodeFunction2)
                    Assert.NotNull(method1)

                    Assert.Equal(True, method1.IsOverloaded)
                    Assert.Equal(19, method1.Overloads.Count)
                End Sub)

        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExternalFunction_Overloads_NotOverloaded() As Task
            Dim code =
<Code>
class $$Derived : System.Console
{
}
</Code>
            Await TestElement(
                code,
                Sub(codeClass)
                    Dim baseType = TryCast(codeClass.Bases.Item(1), EnvDTE80.CodeClass2)
                    Assert.NotNull(baseType)

                    Dim method2 = TryCast(baseType.Members.Item("Clear"), EnvDTE80.CodeFunction2)
                    Assert.NotNull(method2)

                    Assert.Equal(1, method2.Overloads.Count)
                    Assert.Equal("System.Console.Clear", TryCast(method2.Overloads.Item(1), EnvDTE80.CodeFunction2).FullName)
                End Sub)

        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace