' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp

    Public Class CodeNamespaceTests
        Inherits AbstractCodeNamespaceTests

#Region "Remove tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1() As Task
            Dim code =
<Code>
namespace $$Foo
{
    class C
    {
    }
}
</Code>

            Dim expected =
<Code>
namespace Foo
{
}
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

#End Region

        <WorkItem(858153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestChildren1() As Task
            Dim code =
<Code>
namespace N$$
{
    class C1 { }
    class C2 { }
    class C3 { }
}
</Code>

            Await TestChildren(code,
                IsElement("C1", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C2", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C3", EnvDTE.vsCMElement.vsCMElementClass))
        End Function

        <WorkItem(150349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function NoChildrenForInvalidMembers() As Task
            Dim code =
<Code>
namespace N$$
{
    void M() { }
    int P { get { return 42; } }
    event System.EventHandler E;
}
</Code>

            Await TestChildren(code, NoElements)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
namespace $$N
{
}
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Parent", "Members", "DocComment", "Comment"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
