' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeEventTests
        Inherits AbstractCodeEventTests

#Region "GetStartPoint tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint3()
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPointExplicitlyImplementedEvent()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint3()
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPointExplicitlyImplementedEvent()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
class C
{
    private event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
class C
{
    protected event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
class C
{
    protected internal event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
class C
{
    internal event System.EventHandler $$E;
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestFullName(code, "C.E")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestFullName(code, "C.F")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_ExplicitlyImplementedEvent()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsPropertyStyleEvent1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestIsPropertyStyleEvent(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsPropertyStyleEvent2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared1()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestIsShared(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$E, F;
}
</Code>

            TestName(code, "E")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler E, $$F;
}
</Code>

            TestName(code, "F")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/2437")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ExplicitlyImplementedEvent()
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

#Region "OverrideKind tests"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_None()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Goo;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
            Dim code =
<Code>
class C
{
    public event System.EventHandler Goo, $$Bar;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType3()
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Goo
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$Goo;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler Goo;
}
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$Goo;
}
</Code>

            Dim expected =
<Code>
class C
{
    static event System.EventHandler Goo;
}
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$Goo;
}
</Code>

            Dim expected =
<Code>
class C
{
    static event System.EventHandler Goo;
}
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
            Dim code =
<Code>
class C
{
    static event System.EventHandler $$Goo;
}
</Code>

            Dim expected =
<Code>
class C
{
    event System.EventHandler Goo;
}
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

#End Region

#Region "Set Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
class C
{
    event System.EventHandler $$Goo;
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Goo;
}
</Code>

            Dim expected =
<Code>
class C
{
    public event System.ConsoleCancelEventHandler Goo;
}
</Code>

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler Goo, $$Bar;
}
</Code>

            Dim expected =
<Code>
class C
{
    public event System.ConsoleCancelEventHandler Goo, Bar;
}
</Code>

            Await TestSetTypeProp(code, expected, "System.ConsoleCancelEventHandler")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
class C
{
    public event System.EventHandler $$Goo
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
    public event System.ConsoleCancelEventHandler Goo
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    event System.EventHandler $$E;
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeEvent)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
