' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class InterfaceTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WorkItem(546205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromDefinition()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
interface I
{
    void [|$$Goo|]();
}
 
class C : I
{
    void I.[|Goo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(546205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromImplementation()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
interface I
{
    void [|Goo|]();
}
 
class C : I
{
    void I.[|$$Goo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(546205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
namespace N
{
    interface I
    {
        void [|Goo|]();
    }
}
 
class C : N.I
{
    void N.I.[|$$Goo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInterfaceForExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace()
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
namespace N
{
    interface [|I|]
    {
        void Goo();
    }
}
 
class C : N.[|I|]
{
    void N.[|$$I|].Goo() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

    End Class
End Namespace
