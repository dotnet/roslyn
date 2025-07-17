' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class InterfaceTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromDefinition(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromImplementation(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546205")>
        Public Sub RenameExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameInterfaceForExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

    End Class
End Namespace
