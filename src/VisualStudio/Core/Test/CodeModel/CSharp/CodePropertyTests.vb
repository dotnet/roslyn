' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=7, lineOffset:=13, absoluteOffset:=68, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=16)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=13, absoluteOffset:=100, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_AutoProperty() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=25, absoluteOffset:=35, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=30)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_AutoProperty_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P { get; set; }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=20, absoluteOffset:=62, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=25, absoluteOffset:=67, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Indexer() As Task
            Dim code =
<Code>
class C
{
    public int $$this[int index]
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=56, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=7, lineOffset:=13, absoluteOffset:=82, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=30)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_Indexer_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$this[int index]
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=9, absoluteOffset:=88, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=13, absoluteOffset:=114, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=31)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_ExplicitlyImplementedIndexer() As Task
            Dim code =
<Code>
interface I1
{
    int this[int i]
    { get;set; }
}

class C1 : I1
{
    int $$I1.this[int i]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=9, lineOffset:=5, absoluteOffset:=76, lineLength:=22)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_ExplicitlyImplementedProperty() As Task
            Dim code =
<Code>
interface I1
{
    int Prop1 { get; set; }
}

class C1 : I1
{
    int $$I1.Prop1
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=67, lineLength:=16)))
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=12, lineOffset:=1, absoluteOffset:=131, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=1, absoluteOffset:=89, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=12, lineOffset:=6, absoluteOffset:=136, lineLength:=5)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=32, absoluteOffset:=42, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=13, lineOffset:=1, absoluteOffset:=163, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=9, lineOffset:=1, absoluteOffset:=121, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=13, lineOffset:=6, absoluteOffset:=168, lineLength:=5)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_AutoProperty() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=30, absoluteOffset:=40, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=23, absoluteOffset:=33, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=41, lineLength:=30)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_AutoProperty_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P { get; set; }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=32, absoluteOffset:=42, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=30, absoluteOffset:=72, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=23, absoluteOffset:=65, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=4, lineOffset:=31, absoluteOffset:=73, lineLength:=30)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Indexer() As Task
            Dim code =
<Code>
class C
{
    public int $$this[int index]
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=12, lineOffset:=1, absoluteOffset:=145, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=1, absoluteOffset:=103, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=12, lineOffset:=6, absoluteOffset:=150, lineLength:=5)))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_Indexer_Attribute() As Task
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$this[int index]
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=3, lineOffset:=32, absoluteOffset:=42, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=13, lineOffset:=1, absoluteOffset:=177, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=9, lineOffset:=1, absoluteOffset:=135, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=13, lineOffset:=6, absoluteOffset:=182, lineLength:=5)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_ExplicitlyImplementedProperty() As Task
            Dim code =
<Code>
interface I1
{
    int Prop1 { get; set; }
}

class C1 : I1
{
    int $$I1.Prop1
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=18, lineOffset:=6, absoluteOffset:=178, lineLength:=5)))
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_ExplicitlyImplementedIndexer() As Task
            Dim code =
<Code>
interface I1
{
    int this[int i]
    { get;set; }
}

class C1 : I1
{
    int $$I1.this[int i]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=19, lineOffset:=6, absoluteOffset:=193, lineLength:=5)))
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestFullName(code, "C.P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName2() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestFullName(code, "C.P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName3() As Task
            Dim code =
<Code>
class C
{
    public int $$this[int index] { get; set; }
}
</Code>

            Await TestFullName(code, "C.this")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ExplicitlyImplementedProperty() As Task
            Dim code =
<Code>
interface I1
{
    int Prop1 { get; set; }
}

class C1 : I1
{
    int $$I1.Prop1
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestFullName(code, "C1.I1.Prop1")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ExplicitlyImplementedIndexer() As Task
            Dim code =
<Code>
interface I1
{
    int this[int i]
    { get;set; }
}

class C1 : I1
{
    int $$I1.this[int i]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestFullName(code, "C1.I1.this")
        End Function

#End Region

#Region "IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault1() As Task
            Dim code =
<Code>
class C
{
    public int $$this[int index]
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestIsDefault(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault2() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
}
</Code>

            Await TestIsDefault(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsDefault3() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestIsDefault(code, False)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestName(code, "P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestName(code, "P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName3() As Task
            Dim code =
<Code>
class C
{
    public int $$this[int index] { get; set; }
}
</Code>

            Await TestName(code, "this")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ExplicitlyImplementedProperty() As Task
            Dim code =
<Code>
interface I1
{
    int Prop1 { get; set; }
}

class C1 : I1
{
    int $$I1.Prop1
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestName(code, "I1.Prop1")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ExplicitlyImplementedIndexer() As Task
            Dim code =
<Code>
interface I1
{
    int this[int i]
    { get;set; }
}

class C1 : I1
{
    int $$I1.this[int i]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }
}
</Code>

            Await TestName(code, "I1.this")
        End Function

#End Region

#Region "Prototype tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName1() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int this$$[int index] { get; set; }
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.this[int index]")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_ClassName2() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int P$$ { get; set; }
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName1() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int this$$[int index] { get; set; }
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.this[int index]")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestPrototype_FullName2() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        int P$$ { get; set; }
    }
}
</Code>

            Await TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.P")
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Int32",
                             .AsString = "int",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

#End Region

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_None() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
abstract class C
{
    public abstract int $$P
    {
        get;
        set;
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
class C
{
    public virtual int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override() As Task
            Dim code =
<Code>
abstract class B
{
    public abstract int P { get; set; }
}

class C : B
{
    public override int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Sealed() As Task
            Dim code =
<Code>
abstract class B
{
    public abstract int P { get; set; }
}

class C : B
{
    public override sealed int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_New() As Task
            Dim code =
<Code>
abstract class B
{
    public int P { get; set; }
}

class C : B
{
    public new int $$P { get; set; }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override_ExpressionBody() As Task
            Dim code =
<Code>
abstract class A
{
    public abstract int P { get; }
}

class C : A
{
    public override int $$P => 42;
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

#End Region

#Region "ReadWrite tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_GetSet() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Get() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Set() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        set
        {
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_GetSet_AutoProperty() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Get_AutoProperty() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Get_ExpressionBody() As Task
            Dim code =
<Code>
class C
{
    public int $$P => 42;
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

#End Region

#Region "Set OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind_NoneToAbstract() As Task
            Dim code =
<Code>
abstract class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>
            Dim expected =
<Code>
abstract class C
{
    public abstract int P
    {
        get;
        set;
    }
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind_AbstractToNone() As Task
            Dim code =
<Code>
abstract class C
{
    public abstract int $$P
    {
        get;
        set;
    }
}
</Code>
            Dim expected =
<Code>
abstract class C
{
    public int P
    {
        get
        {
        }
        set
        {
        }
    }
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind_NoneToOverride_ExpressionBody() As Task
            Dim code =
<Code>
class C
{
    public int $$P => 42;
}
</Code>
            Dim expected =
<Code>
class C
{
    public override int P => 42;
}
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

#End Region

#Region "Set IsDefault tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault1() As Task
            Dim code =
<Code>
abstract class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Dim expected =
<Code>
abstract class C
{
    public int P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestSetIsDefault(code, expected, True, ThrowsInvalidOperationException(Of Boolean))
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Dim expected =
<Code>
class C
{
    public string P { get; set; }
}
</Code>

            Await TestSetTypeProp(code, expected, "string")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            Dim expected =
<Code>
class C
{
    public string P { get; set; }
}
</Code>

            Await TestSetTypeProp(code, expected, "System.String")
        End Function

#End Region

#Region "AutoImplementedPropertyExtender"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented1() As Task
            Dim code =
<Code>
public class C
{
    int $$P { get; set; }
}
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented2() As Task
            Dim code =
<Code>
public class C
{
    int $$P
    {
        get { return 0; }
        set { }
    }
}
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAutoImplementedPropertyExtender_IsAutoImplemented3() As Task
            Dim code =
<Code>
public interface I
{
    int $$P { get; set; }
}
</Code>

            Await TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters() As Task
            Dim code =
<Code>
class Program
{
    public int $$this[int @int]
    {
        get { return @int; }
        set { }
    }
}
</Code>

            Await TestAllParameterNames(code, "@int")
        End Function

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

class C
{
    public int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable()]
    public int P
    {
        get { return default(int); }
        set { }
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    public int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable]
    [CLSCompliant(true)]
    public int P
    {
        get { return default(int); }
        set { }
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    public int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    [CLSCompliant(true)]
    public int P
    {
        get { return default(int); }
        set { }
    }
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
class C
{
    int $$P { get { return 42; } }
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Type", "Getter", "Setter", "Access", "Attributes",
                 "DocComment", "Comment", "Parameters", "IsGeneric", "OverrideKind", "IsShared",
                 "IsDefault", "Parent2", "ReadWrite"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Private Function GetAutoImplementedPropertyExtender(codeElement As EnvDTE80.CodeProperty2) As ICSAutoImplementedPropertyExtender
            Return CType(codeElement.Extender(ExtenderNames.AutoImplementedProperty), ICSAutoImplementedPropertyExtender)
        End Function

        Protected Overrides Function AutoImplementedPropertyExtender_GetIsAutoImplemented(codeElement As EnvDTE80.CodeProperty2) As Boolean
            Return GetAutoImplementedPropertyExtender(codeElement).IsAutoImplemented
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
