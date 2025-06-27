' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class OverrideTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameOverrideMemberFromDerivedClass(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
namespace ClassLibrary1
{
    public class Class1
    {
        public virtual void [|M|]() { }
    }
}
                        </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
                            <ProjectReference>ClassLibrary1</ProjectReference>
                            <Document>
namespace ClassLibrary2
{
    public class Class2 : ClassLibrary1.Class1
    {
        public override void [|$$M|]() { }
    }
}
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25682")>
        Public Sub RenameOverrideMemberFromDerivedClassWhenMemberIsPrivate(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
namespace ClassLibrary1
{
    public class Class1
    {
        public virtual void [|M|]() { }
    }
}
                        </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
                            <ProjectReference>ClassLibrary1</ProjectReference>
                            <Document>
namespace ClassLibrary2
{
    public class Class2 : ClassLibrary1.Class1
    {
        override void [|$$M|]() { }
    }
}
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameOverrideMemberFromDerivedClass_abstract_virtual(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
namespace ClassLibrary1
{
    public abstract class Class1
    {
        public abstract void M();
    }
}
                        </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
                            <ProjectReference>ClassLibrary1</ProjectReference>
                            <Document>
namespace ClassLibrary2
{
    public class Class2 : ClassLibrary1.Class1
    {
        virtual void [|$$M|]() { }
    }
}
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameOverrideMemberFromDerivedClass_abstract_override(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
namespace ClassLibrary1
{
    public abstract class Class1
    {
        public virtual void [|M|]();
    }
}
                        </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ClassLibrary2" CommonReferences="true">
                            <ProjectReference>ClassLibrary1</ProjectReference>
                            <Document>
namespace ClassLibrary2
{
    public class Class2 : ClassLibrary1.Class1
    {
        override void [|$$M|]() { }
    }
}
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub
    End Class
End Namespace
