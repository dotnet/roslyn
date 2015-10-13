Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToImplementation
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToImplementation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToImplementation
    Public Class GoToImplementationTests
        Private Sub Test(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True)
            GoToTestHelpers.Test(workspaceDefinition, shouldSucceed,
                Function(document As Document, cursorPosition As Integer, presenters As IEnumerable(Of Lazy(Of INavigableItemsPresenter)))
                    Dim service = If(document.Project.Language = LanguageNames.CSharp,
                        DirectCast(New CSharpGoToImplementationService(presenters), IGoToImplementationService),
                        New VisualBasicGoToImplementationService(presenters))

                    Dim message As String = Nothing
                    Return service.TryGoToImplementation(document, cursorPosition, CancellationToken.None, message)
                End Function)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestEmptyFile()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
$$
        </Document>
    </Project>
</Workspace>

            Test(workspace, shouldSucceed:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithSingleClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|] { }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithSingleClassImplementation()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithTwoClassImplementations()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
class [|D|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithOneMethodImplementation()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithTwoMethodImplementations()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
class D : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithNonInheritedImplementation()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
class D : C, I { }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithInterfaceMemberFromMetdataAtUseSite()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C : IDisposable
{
    public void [|Dispose|]()
    {
        IDisposable d;
        d.$$Dispose();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithSimpleMethod()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithOverridableMethodOnBase()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void [|$$M|]() { }
}

class D : C
{
    public override void [|M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Sub TestWithOverridableMethodOnImplementation()
            ' Our philosophy is to only show derived in this case, since we know the implementation of 
            ' D could never call C.M here
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C 
{
    public virtual void M() { }
}

class D : C
{
    public override void [|$$M|]() { }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
    End Class
End Namespace