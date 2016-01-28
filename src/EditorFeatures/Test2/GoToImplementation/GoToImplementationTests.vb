' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToImplementation
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToImplementation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToImplementation
    Public Class GoToImplementationTests
        Private Function TestAsync(workspaceDefinition As XElement, Optional shouldSucceed As Boolean = True) As Tasks.Task
            Return GoToTestHelpers.TestAsync(workspaceDefinition, shouldSucceed,
                Function(document As Document, cursorPosition As Integer, presenters As IEnumerable(Of Lazy(Of INavigableItemsPresenter)))
                    Dim service = If(document.Project.Language = LanguageNames.CSharp,
                        DirectCast(New CSharpGoToImplementationService(presenters), IGoToImplementationService),
                        New VisualBasicGoToImplementationService(presenters))

                    Dim message As String = Nothing
                    Return service.TryGoToImplementation(document, cursorPosition, CancellationToken.None, message)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestEmptyFile() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
$$
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, shouldSucceed:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|$$C|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSingleClassImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] : I { }
interface $$I { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoClassImplementations() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOneMethodImplementation() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C : I { public void [|M|]() { } }
interface I { void $$M(); }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithTwoMethodImplementations() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithNonInheritedImplementation() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithInterfaceMemberFromMetdataAtUseSite() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithSimpleMethod() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnBase() As Task
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

            Await TestAsync(workspace)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToImplementation)>
        Public Async Function TestWithOverridableMethodOnImplementation() As Task
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

            Await TestAsync(workspace)
        End Function
    End Class
End Namespace