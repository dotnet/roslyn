' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeEnumTests
        Inherits AbstractCodeEnumTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess1() As Task
            Dim code =
<Code>
enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess2() As Task
            Dim code =
<Code>
class C
{
    enum $$E { }
}
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess3() As Task
            Dim code =
<Code>
private enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess4() As Task
            Dim code =
<Code>
protected enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess5() As Task
            Dim code =
<Code>
protected internal enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess6() As Task
            Dim code =
<Code>
internal enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAccess7() As Task
            Dim code =
<Code>
public enum $$E { }
</Code>

            Await TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

#End Region

#Region "Bases tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestBases1() As Task
            Dim code =
<Code>
enum $$E
{
    Foo = 1,
    Bar
}</Code>

            Await TestBases(code, IsElement("Enum"))
        End Function

#End Region

#Region "Attributes tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes1() As Task
            Dim code =
<Code>
enum $$C
{
    Foo = 1
}
</Code>

            Await TestAttributes(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes2() As Task
            Dim code =
<Code>
using System;

[Flags]
enum $$C
{
    Foo = 1
}
</Code>

            Await TestAttributes(code, IsElement("Flags"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes3() As Task
            Dim code =
<Code>using System;

[Serializable]
[Flags]
enum $$C
{
    Foo = 1
}
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("Flags"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAttributes4() As Task
            Dim code =
<Code>using System;

[Serializable, Flags]
enum $$C
{
    Foo = 1
}
</Code>

            Await TestAttributes(code, IsElement("Serializable"), IsElement("Flags"))
        End Function
#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
enum $$E
{
    Foo = 1,
    Bar
}</Code>

            Await TestFullName(code, "E")
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
enum $$E
{
    Foo = 1,
    Bar
}
</Code>

            Await TestName(code, "E")
        End Function

#End Region

#Region "AddAttribute tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
using System;

enum $$E
{
    Foo = 1,
    Bar
}
</Code>

            Dim expected =
<Code>
using System;

[Flags()]
enum E
{
    Foo = 1,
    Bar
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
using System;

[Flags]
enum $$E
{
    Foo = 1,
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
    Foo = 1,
    Bar
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

/// &lt;summary&gt;&lt;/summary&gt;
enum $$E
{
    Foo = 1,
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
    Foo = 1,
    Bar
}
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Flags"})
        End Function

#End Region

#Region "AddMember tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(638225)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(638225)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(638225)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem(638225)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
enum $$Foo
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
enum $$E
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Namespace", "Bases", "Members", "Access", "Attributes",
                 "DocComment", "Comment", "DerivedTypes"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
