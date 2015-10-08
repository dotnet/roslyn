' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeEventTests
        Inherits AbstractCodeEventTests

#Region "GetStartPoint tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=38, absoluteOffset:=48, lineLength:=42)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=42)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=41, absoluteOffset:=51, lineLength:=42)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=42)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint3()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E
    {
        add { }
        remove { }
    }
}
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=5, lineOffset:=9, absoluteOffset:=64, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=14, absoluteOffset:=69, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=15, lineLength:=38)))
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPointExplicitlyImplementedEvent()
            Dim code =
<Code>
delegate void SampleEventHandler(object sender);

interface I1
{
    event SampleEventHandler SampleEvent;
}

class C1 : I1
{
    event SampleEventHandler $$I1.SampleEvent
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

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=10, lineOffset:=5, absoluteOffset:=131, lineLength:=43)))
        End Sub

#End Region

#Region "GetEndPoint tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=39, absoluteOffset:=49, lineLength:=42)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=43, absoluteOffset:=53, lineLength:=42)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=42, absoluteOffset:=52, lineLength:=42)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=43, absoluteOffset:=53, lineLength:=42)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint3()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E
    {
        add { }
        remove { }
    }
}
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     ThrowsCOMException(E_FAIL)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=7, lineOffset:=1, absoluteOffset:=91, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=5, lineOffset:=15, absoluteOffset:=70, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     ThrowsNotImplementedException),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=7, lineOffset:=6, absoluteOffset:=96, lineLength:=5)))
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPointExplicitlyImplementedEvent()
            Dim code =
<Code>
delegate void SampleEventHandler(object sender);

interface I1
{
    event SampleEventHandler SampleEvent;
}

class C1 : I1
{
    event SampleEventHandler $$I1.SampleEvent
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

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=19, lineOffset:=6, absoluteOffset:=250, lineLength:=5)))
        End Sub

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
<Code>
class C
{
    private event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
<Code>
class C
{
    protected event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
<Code>
class C
{
    protected internal event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
<Code>
class C
{
    internal event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestFullName(code, "C.E")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestFullName(code, "C.F")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName_ExplicitlyImplementedEvent()
            Dim code =
<Code>
delegate void SampleEventHandler(object sender);

interface I1
{
    event SampleEventHandler SampleEvent;
}

class C1 : I1
{
    event SampleEventHandler $$I1.SampleEvent
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

            TestFullName(code, "C1.I1.SampleEvent")
        End Sub

#End Region

#Region "IsPropertyStyleEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsPropertyStyleEvent1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsPropertyStyleEvent2()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E
    {
        add { }
        remove { }
    }
}
</Code>

            TestIsPropertyStyleEvent(code, True)
        End Sub

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$E;
}
</Code>

            TestIsShared(code, True)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestName(code, "E")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestName(code, "F")
        End Sub

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name_ExplicitlyImplementedEvent()
            Dim code =
<Code>
delegate void SampleEventHandler(object sender);

interface I1
{
    event SampleEventHandler SampleEvent;
}

class C1 : I1
{
    event SampleEventHandler $$I1.SampleEvent
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

            TestName(code, "I1.SampleEvent")
        End Sub

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Foo;
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler Foo, $$Bar;
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type3()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Foo
    {
        add { }
        remove { }
    }
}
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler Foo;
}
</Code>

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    static event System.EventHandler Foo;
}
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared3()
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    static event System.EventHandler Foo;
}
</Code>

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared4()
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler Foo;
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
class C
{
    event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler Bar;
}
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Foo;
}
</Code>

            Dim expected =
<Code>
class C
{
    public event System.ConsoleCancelEventHandler Foo;
}
</Code>

            TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler Foo, $$Bar;
}
</Code>

            Dim expected =
<Code>
class C
{
    public event System.ConsoleCancelEventHandler Foo, Bar;
}
</Code>

            TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Foo
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
    public event System.ConsoleCancelEventHandler Foo
    {
        add { }
        remove { }
    }
}
</Code>

            TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Sub

#End Region

#Region "AddAttribute tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
using System;

class C
{
    public event EventHandler $$E;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable()]
    public event EventHandler E;
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
using System;

class C
{
    [Serializable]
    public event EventHandler $$E;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    [Serializable]
    [CLSCompliant(true)]
    public event EventHandler E;
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
            Dim code =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    public event EventHandler $$E;
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
    /// &lt;summary&gt;&lt;/summary&gt;
    [CLSCompliant(true)]
    public event EventHandler E;
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Access", "Attributes", "DocComment", "Comment", "Adder",
                 "Remover", "Thrower", "IsPropertyStyleEvent", "Type", "OverrideKind", "IsShared"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
