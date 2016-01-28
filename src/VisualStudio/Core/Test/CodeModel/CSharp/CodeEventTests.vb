' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeEventTests
        Inherits AbstractCodeEventTests

#Region "GetStartPoint tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint3() As Task
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

            Await TestGetStartPoint(code,
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
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPointExplicitlyImplementedEvent() As Task
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

            Await TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=10, lineOffset:=5, absoluteOffset:=131, lineLength:=43)))
        End Function

#End Region

#Region "GetEndPoint tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint3() As Task
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

            Await TestGetEndPoint(code,
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
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPointExplicitlyImplementedEvent() As Task
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

            Await TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=19, lineOffset:=6, absoluteOffset:=250, lineLength:=5)))
        End Function

#End Region

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
class C
{
    private event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
class C
{
    protected event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
class C
{
    protected internal event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
class C
{
    internal event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E;
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            Await TestFullName(code, "C.E")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            Await TestFullName(code, "C.F")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_ExplicitlyImplementedEvent() As Task
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

            Await TestFullName(code, "C1.I1.SampleEvent")
        End Function

#End Region

#Region "IsPropertyStyleEvent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsPropertyStyleEvent1() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            Await TestIsPropertyStyleEvent(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsPropertyStyleEvent2() As Task
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

            Await TestIsPropertyStyleEvent(code, True)
        End Function

#End Region

#Region "IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared1() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            Await TestIsShared(code, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestIsShared2() As Task
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$E;
}
</Code>

            Await TestIsShared(code, True)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            Await TestName(code, "E")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            Await TestName(code, "F")
        End Function

        <WorkItem(2437, "https://github.com/dotnet/roslyn/issues/2437")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ExplicitlyImplementedEvent() As Task
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

            Await TestName(code, "I1.SampleEvent")
        End Function

#End Region

#Region "OverrideKind tests"

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
class C
{
    abstract class A
    {
        protected abstract event System.EventHandler $$E;
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
class C
{
    abstract class A
    {
        protected virtual event System.EventHandler $$E
        {
            add { }
            remove { }
        }
    }
}
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_None() As Task
            Dim code =
<Code>
class C
{
    abstract class A
    {
        protected event System.EventHandler $$E
        {
            add { }
            remove { }
        }
    }
}
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Foo;
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler Foo, $$Bar;
}
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType3() As Task
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

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "System.EventHandler",
                             .AsFullName = "System.EventHandler",
                             .CodeTypeFullName = "System.EventHandler",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Function

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
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

            Await TestSetIsShared(code, expected, False)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
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

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
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

            Await TestSetIsShared(code, expected, True)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
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

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
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

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
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

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
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

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
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

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
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

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
