' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
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

            TestGetStartPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoPropGetStartPoint1()
            Dim code =
<Code>
class C
{
    public int $$P => 0;
}
</Code>

            TestElement(
                code,
                Sub(prop)
                    Dim getter = prop.Getter
                    Dim textPointGetter = Function(part As EnvDTE.vsCMPart)
                                              Return getter.GetStartPoint(part)
                                          End Function

                    Part(EnvDTE.vsCMPart.vsCMPartAttributes, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartBody, TextPoint(line:=3, lineOffset:=21, absoluteOffset:=31, lineLength:=22))(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartHeader, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartName, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartNavigate, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartWhole, TextPoint(line:=3, lineOffset:=18, absoluteOffset:=28, lineLength:=22))(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes, ThrowsNotImplementedException)(textPointGetter)
                End Sub)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoPropGetEndPoint1()
            Dim code =
<Code>
class C
{
    public int $$P => 0;
}
</Code>

            TestElement(
                code,
                Sub(prop)
                    Dim getter = prop.Getter
                    Dim textPointGetter = Function(part As EnvDTE.vsCMPart)
                                              Return getter.GetEndPoint(part)
                                          End Function

                    Part(EnvDTE.vsCMPart.vsCMPartAttributes, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartBody, TextPoint(line:=3, lineOffset:=22, absoluteOffset:=32, lineLength:=22))(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartHeader, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartName, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartNavigate, ThrowsNotImplementedException)(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartWhole, TextPoint(line:=3, lineOffset:=22, absoluteOffset:=32, lineLength:=22))(textPointGetter)
                    Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes, ThrowsNotImplementedException)(textPointGetter)
                End Sub)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Attribute()
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

            TestGetStartPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_AutoProperty()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestGetStartPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_AutoProperty_Attribute()
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P { get; set; }
}
</Code>

            TestGetStartPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Indexer()
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

            TestGetStartPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Indexer_Attribute()
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

            TestGetStartPoint(code,
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
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_ExplicitlyImplementedIndexer()
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

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=9, lineOffset:=5, absoluteOffset:=76, lineLength:=22)))
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_ExplicitlyImplementedProperty()
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

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=67, lineLength:=16)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
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

            TestGetEndPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Attribute()
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

            TestGetEndPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_AutoProperty()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestGetEndPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_AutoProperty_Attribute()
            Dim code =
<Code>
class C
{
    [System.CLSCompliant(true)]
    public int $$P { get; set; }
}
</Code>

            TestGetEndPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Indexer()
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

            TestGetEndPoint(code,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Indexer_Attribute()
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

            TestGetEndPoint(code,
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
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_ExplicitlyImplementedProperty()
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

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=18, lineOffset:=6, absoluteOffset:=178, lineLength:=5)))
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_ExplicitlyImplementedIndexer()
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

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=19, lineOffset:=6, absoluteOffset:=193, lineLength:=5)))
        End Sub

#End Region

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
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

            TestFullName(code, "C.P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestFullName(code, "C.P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName3()
            Dim code =
<Code>
class C
{
    public int $$this[int index] { get; set; }
}
</Code>

            TestFullName(code, "C.this")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ExplicitlyImplementedProperty()
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

            TestFullName(code, "C1.I1.Prop1")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ExplicitlyImplementedIndexer()
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

            TestFullName(code, "C1.I1.this")
        End Sub

#End Region

#Region "IsDefault tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault1()
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

            TestIsDefault(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault2()
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

            TestIsDefault(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault3()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestIsDefault(code, False)
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
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

            TestName(code, "P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestName(code, "P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName3()
            Dim code =
<Code>
class C
{
    public int $$this[int index] { get; set; }
}
</Code>

            TestName(code, "this")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ExplicitlyImplementedProperty()
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

            TestName(code, "I1.Prop1")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ExplicitlyImplementedIndexer()
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

            TestName(code, "I1.this")
        End Sub

#End Region

#Region "Prototype tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName1()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.this[int index]")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName2()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName1()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.this[int index]")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName2()
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

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.P")
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
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

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Int32",
                             .AsString = "int",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

#End Region

#Region "OverrideKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_None()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Sealed()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_New()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override_ExpressionBody()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

#End Region

#Region "ReadWrite tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_GetSet()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Set()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_GetSet_AutoProperty()
            Dim code =
<Code>
class C
{
    public int $$P { get; set; }
}
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get_AutoProperty()
            Dim code =
<Code>
class C
{
    public int $$P { get; }
}
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get_ExpressionBody()
            Dim code =
<Code>
class C
{
    public int $$P => 42;
}
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

#End Region

#Region "Set OverrideKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented1()
            Dim code =
<Code>
public class C
{
    int $$P { get; set; }
}
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented2()
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

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented3()
            Dim code =
<Code>
public interface I
{
    int $$P { get; set; }
}
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
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

            TestAllParameterNames(code, "@int")
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    int $$P { get { return 42; } }
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeProperty2)(code)
        End Sub

        Private Shared Function GetAutoImplementedPropertyExtender(codeElement As EnvDTE80.CodeProperty2) As ICSAutoImplementedPropertyExtender
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
