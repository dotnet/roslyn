' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp

    Public Class CodeNamespaceTests
        Inherits AbstractCodeNamespaceTests

#Region "Comment tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment1()
            Dim code =
<Code>
namespace $$N { }
</Code>

            TestComment(code, String.Empty)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment2()
            Dim code =
<Code>
// Goo
// Bar
namespace $$N { }
</Code>

            TestComment(code, "Goo" & vbCrLf & "Bar" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment3()
            Dim code =
<Code>
namespace N1 { } // Goo
// Bar
namespace $$N2 { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment4()
            Dim code =
<Code>
namespace N1 { } // Goo
/* Bar */
namespace $$N2 { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment5()
            Dim code =
<Code>
namespace N1 { } // Goo
/*
    Bar
*/
namespace $$N2 { }
</Code>

            TestComment(code, "Bar" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment6()
            Dim code =
<Code>
namespace N1 { } // Goo
/*
    Hello
    World!
*/
namespace $$N2 { }
</Code>

            TestComment(code, "Hello" & vbCrLf & "World!" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment7()
            Dim code =
<Code>
namespace N1 { } // Goo
/*
    Hello
    
    World!
*/
namespace $$N2 { }
</Code>

            TestComment(code, "Hello" & vbCrLf & vbCrLf & "World!" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment8()
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

            TestComment(code, "This" & vbCrLf & "is" & vbCrLf & "a" & vbCrLf & "multi-line" & vbCrLf & "comment!" & vbCrLf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment9()
            Dim code =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
namespace $$N { }
</Code>

            TestComment(code, String.Empty)
        End Sub

#End Region

#Region "DocComment tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment1()
            Dim code =
<Code>
/// &lt;summary&gt;Hello World&lt;/summary&gt;
namespace $$N { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>Hello World</summary>" & vbCrLf & "</doc>")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment2()
            Dim code =
<Code>
/// &lt;summary&gt;
/// Hello World
/// &lt;/summary&gt;
namespace $$N { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment3()
            Dim code =
<Code>
///    &lt;summary&gt;
/// Hello World
///&lt;/summary&gt;
namespace $$N { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "    <summary>" & vbCrLf & " Hello World" & vbCrLf & "</summary>" & vbCrLf & "</doc>")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment4()
            Dim code =
<Code>
/// &lt;summary&gt;
/// Summary
/// &lt;/summary&gt;
/// &lt;remarks&gt;Remarks&lt;/remarks&gt;
namespace $$N { }
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "<summary>" & vbCrLf & "Summary" & vbCrLf & "</summary>" & vbCrLf & "<remarks>Remarks</remarks>" & vbCrLf & "</doc>")
        End Sub

#End Region

#Region "Set Comment tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment1() As Task
            Dim code =
<Code>
// Goo

// Bar
namespace $$N { }
</Code>

            Dim expected =
<Code>
namespace N { }
</Code>

            Await TestSetComment(code, expected, Nothing)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment2() As Task
            Dim code =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
namespace $$N { }
</Code>

            Dim expected =
<Code>
// Goo
/// &lt;summary&gt;Bar&lt;/summary&gt;
// Bar
namespace N { }
</Code>

            Await TestSetComment(code, expected, "Bar")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
            Dim code =
<Code>
// Goo

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment3() As Task
            Dim code =
<Code>
// Goo
namespace $$N { }
</Code>

            Dim expected =
<Code>
// Goo
/// &lt;summary&gt;Blah&lt;/summary&gt;
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment4() As Task
            Dim code =
<Code>
/// &lt;summary&gt;FogBar&lt;/summary&gt;
// Goo
namespace $$N { }
</Code>

            Dim expected =
<Code>
/// &lt;summary&gt;Blah&lt;/summary&gt;
// Goo
namespace N { }
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</summary></doc>")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

#Region "Set Name tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SameName() As Task
            Dim code =
<Code><![CDATA[
namespace N$$ 
{
    class C
    {
    }
}
]]></Code>

            Dim expected =
<Code><![CDATA[
namespace N
{
    class C
    {
    }
}
]]></Code>

            Await TestSetName(code, expected, "N", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_NewName() As Task
            Dim code =
<Code><![CDATA[
namespace N$$
{
    class C
    {
    }
}
]]></Code>

            Dim expected =
<Code><![CDATA[
namespace N2
{
    class C
    {
    }
}
]]></Code>

            Await TestSetName(code, expected, "N2", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_NewName_FileScopedNamespace() As Task
            Dim code =
<Code><![CDATA[
namespace N$$;

class C
{
}
]]></Code>

            Dim expected =
<Code><![CDATA[
namespace N2;

class C
{
}
]]></Code>

            Await TestSetName(code, expected, "N2", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SimpleNameToDottedName() As Task
            Dim code =
<Code><![CDATA[
namespace N1$$
{
    class C
    {
    }
}
]]></Code>

            Dim expected =
<Code><![CDATA[
namespace N2.N3
{
    class C
    {
    }
}
]]></Code>

            Await TestSetName(code, expected, "N2.N3", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_DottedNameToSimpleName() As Task
            Dim code =
<Code><![CDATA[
namespace N1.N2$$
{
    class C
    {
    }
}
]]></Code>

            Dim expected =
<Code><![CDATA[
namespace N3.N4
{
    class C
    {
    }
}
]]></Code>

            Await TestSetName(code, expected, "N3.N4", NoThrow(Of String)())
        End Function

#End Region

#Region "Remove tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1() As Task
            Dim code =
<Code>
namespace $$Goo
{
    class C
    {
    }
}
</Code>

            Dim expected =
<Code>
namespace Goo
{
}
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1_FileScopedNamespace() As Task
            Dim code =
<Code>
namespace $$Goo;

class C
{
}
</Code>

            Dim expected =
<Code>
namespace Goo;

</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

#End Region

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren1()
            Dim code =
<Code>
namespace N$$
{
    class C1 { }
    class C2 { }
    class C3 { }
}
</Code>

            TestChildren(code,
                IsElement("C1", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C2", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C3", EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren1_FileScopedNamespace()
            Dim code =
<Code>
namespace N$$;

class C1 { }
class C2 { }
class C3 { }
</Code>

            TestChildren(code,
                IsElement("C1", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C2", EnvDTE.vsCMElement.vsCMElementClass),
                IsElement("C3", EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub NoChildrenForInvalidMembers()
            Dim code =
<Code>
namespace N$$
{
    void M() { }
    int P { get { return 42; } }
    event System.EventHandler E;
}
</Code>

            TestChildren(code, NoElements)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
namespace $$N
{
}
</Code>

            TestPropertyDescriptors(Of EnvDTE.CodeNamespace)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
