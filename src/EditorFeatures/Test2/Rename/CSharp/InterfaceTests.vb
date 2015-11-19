' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class InterfaceTests
        <WorkItem(546205)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromDefinition()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
interface I
{
    void [|$$Foo|]();
}
 
class C : I
{
    void I.[|Foo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(546205)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberFromImplementation()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
interface I
{
    void [|Foo|]();
}
 
class C : I
{
    void I.[|$$Foo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WorkItem(546205)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
namespace N
{
    interface I
    {
        void [|Foo|]();
    }
}
 
class C : N.I
{
    void N.I.[|$$Foo|]() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInterfaceForExplicitlyImplementedInterfaceMemberWithInterfaceInNamespace()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
namespace N
{
    interface [|I|]
    {
        void Foo();
    }
}
 
class C : N.[|I|]
{
    void N.[|$$I|].Foo() { }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

    End Class
End Namespace
