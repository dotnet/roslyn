' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp

    Public Class CodeNamespaceTests
        Inherits AbstractCodeNamespaceTests

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment1() As Task
            Dim code =
<Code>
namespace $$N { }
</Code>

            Await TestComment(code, String.Empty)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment2() As Task
            Dim code =
<Code>
// Foo
// Bar
namespace $$N { }
</Code>

            Await TestComment(code, "Foo" & vbCrLf & "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment3() As Task
            Dim code =
<Code>
namespace N1 { } // Foo
// Bar
namespace $$N2 { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment4() As Task
            Dim code =
<Code>
namespace N1 { } // Foo
/* Bar */
namespace $$N2 { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment5() As Task
            Dim code =
<Code>
namespace N1 { } // Foo
/*
    Bar
*/
namespace $$N2 { }
</Code>

            Await TestComment(code, "Bar" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment6() As Task
            Dim code =
<Code>
namespace N1 { } // Foo
/*
    Hello
    World!
*/
namespace $$N2 { }
</Code>

            Await TestComment(code, "Hello" & vbCrLf & "World!" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment7() As Task
            Dim code =
<Code>
namespace N1 { } // Foo
/*
    Hello
    
    World!
*/
namespace $$N2 { }
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
namespace $$N { }
</Code>

            Await TestComment(code, "This" & vbCrLf & "is" & vbCrLf & "a" & vbCrLf & "multi-line" & vbCrLf & "comment!" & vbCrLf)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestComment9() As Task
            Dim code =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
namespace $$N { }
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
namespace $$N { }
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
namespace $$N { }
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
namespace $$N { }
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
namespace $$N { }
</Code>

            Await TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Summary" & vbCrLf & "</summary>" & vbCrLf & "<remarks>Remarks</remarks>" & vbCrLf & "</doc>")
        End Function

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment1() As Task
            Dim code =
<Code>
// Foo

// Bar
namespace $$N { }
</Code>

            Dim expected =
<Code>
namespace N { }
</Code>

            Await TestSetComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment2() As Task
            Dim code =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
namespace $$N { }
</Code>

            Dim expected =
<Code>
// Foo
/// &lt;summary&gt;Bar&lt;/summary&gt;
// Bar
namespace N { }
</Code>

            Await TestSetComment(code, expected, "Bar")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
            Dim code =
<Code>
// Foo

// Bar
namespace $$N { }
</Code>

            Dim expected =
<Code>
// Blah
namespace N { }
</Code>

            Await TestSetComment(code, expected, "Blah")
        End Function

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing() As Task
            Dim code =
<Code>
namespace $$N { }
</Code>

            Dim expected =
<Code>
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, Nothing, ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml1() As Task
            Dim code =
<Code>
namespace $$N { }
</Code>

            Dim expected =
<Code>
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</doc>", ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml2() As Task
            Dim code =
<Code>
namespace $$N { }
</Code>

            Dim expected =
<Code>
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>", ThrowsArgumentException(Of String))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment1() As Task
            Dim code =
<Code>
namespace $$N { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment2() As Task
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
namespace $$N { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment3() As Task
            Dim code =
<Code>
// Foo
namespace $$N { }
</Code>

            Dim expected =
<Code>
// Foo
/// &lt;summary&gt;Blah&lt;/summary&gt;
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment4() As Task
            Dim code =
<Code>
/// &lt;summary&gt;FogBar&lt;/summary&gt;
// Foo
namespace $$N { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
// Foo
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment5() As Task
            Dim code =
<Code>
namespace N1
{
    namespace $$N2 { }
}
</Code>

            Dim expected =
<Code>
namespace N1
{
    /// &lt;summary&gt;Hello World&lt;/summary&gt;
    namespace N2 { }
}
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Hello World</summary></doc>")
        End Function

#End Region

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
