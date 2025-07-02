' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeEnumTests
        Inherits AbstractCodeEnumTests

#Region "Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
<Code>
enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
<Code>
class C
{
    enum $$E { }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
<Code>
private enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
<Code>
protected enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
<Code>
protected internal enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
<Code>
internal enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
<Code>
public enum $$E { }
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Bases tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBases1()
            Dim code =
<Code>
enum $$E
{
    Goo = 1,
    Bar
}</Code>

            TestBases(code, IsElement("Enum"))
        End Sub

#End Region

#Region "Attributes tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
enum $$C
{
    Goo = 1
}
</Code>

            TestAttributes(code, NoElements)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes2()
            Dim code =
<Code>
using System;

[Flags]
enum $$C
{
    Goo = 1
}
</Code>

            TestAttributes(code, IsElement("Flags"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes3()
            Dim code =
<Code>using System;

[Serializable]
[Flags]
enum $$C
{
    Goo = 1
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("Flags"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes4()
            Dim code =
<Code>using System;

[Serializable, Flags]
enum $$C
{
    Goo = 1
}
</Code>

            TestAttributes(code, IsElement("Serializable"), IsElement("Flags"))
        End Sub
#End Region

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
enum $$E
{
    Goo = 1,
    Bar
}</Code>

            TestFullName(code, "E")
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
enum $$E
{
    Goo = 1,
    Bar
}
</Code>

            TestName(code, "E")
        End Sub

#End Region

#Region "AddAttribute tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

enum $$E
{
    Goo = 1,
    Bar
}
</Code>

            Dim expected =
<Code>
using System;

[Flags()]
enum E
{
    Goo = 1,
    Bar
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

[Flags]
enum $$E
{
    Goo = 1,
    Bar
}
</Code>

            Dim expected =
<Code>
using System;

[Flags]
[CLSCompliant(true)]
enum E
{
    Goo = 1,
    Bar
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

/// &lt;summary&gt;&lt;/summary&gt;
enum $$E
{
    Goo = 1,
    Bar
}
</Code>

            Dim expected =
<Code>
using System;

/// &lt;summary&gt;&lt;/summary&gt;
[Flags()]
enum E
{
    Goo = 1,
    Bar
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

#End Region

#Region "AddMember tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember1() As Task
            Dim code =
<Code>
enum $$E
{
}
</Code>

            Dim expected =
<Code>
enum E
{
    V
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Name = "V"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember2() As Task
            Dim code =
<Code>
enum $$E
{
}
</Code>

            Dim expected =
<Code>
enum E
{
    V = 1
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Name = "V", .Value = "1"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember3() As Task
            Dim code =
<Code>
enum $$E
{
    V
}
</Code>

            Dim expected =
<Code>
enum E
{
    U = V,
    V
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Name = "U", .Value = "V"})
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638225")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember4() As Task
            Dim code =
<Code>
enum $$E
{
    A
}
</Code>

            Dim expected =
<Code>
enum E
{
    A,
    B
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Position = -1, .Name = "B"})
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638225")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember5() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    C
}
</Code>

            Dim expected =
<Code>
enum E
{
    A,
    B,
    C
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Position = 1, .Name = "B"})
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638225")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember6() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    B
}
</Code>

            Dim expected =
<Code>
enum E
{
    A,
    B,
    C
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Position = -1, .Name = "C"})
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638225")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddMember7() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    B,
    C
}
</Code>

            Dim expected =
<Code>
enum E
{
    A,
    B,
    C,
    D
}
</Code>

            Await TestAddEnumMember(code, expected, New EnumMemberData With {.Position = -1, .Name = "D"})
        End Function

#End Region

#Region "RemoveMember tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember1() As Task
            Dim code =
<Code>
enum $$E
{
    A
}
</Code>

            Dim expected =
<Code>
enum E
{
}
</Code>

            Await TestRemoveChild(code, expected, "A")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember2() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    B
}
</Code>

            Dim expected =
<Code>
enum E
{
    B
}
</Code>

            Await TestRemoveChild(code, expected, "A")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember3() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    B
}
</Code>

            Dim expected =
<Code>
enum E
{
    A
}
</Code>

            Await TestRemoveChild(code, expected, "B")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveMember4() As Task
            Dim code =
<Code>
enum $$E
{
    A,
    B,
    C
}
</Code>

            Dim expected =
<Code>
enum E
{
    A,
    C
}
</Code>

            Await TestRemoveChild(code, expected, "B")
        End Function

#End Region

#Region "Set Name tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
enum $$Goo
{
}
</Code>

            Dim expected =
<Code>
enum Bar
{
}
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function
#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
enum $$E
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE.CodeEnum)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
