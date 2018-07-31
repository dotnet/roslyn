' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class OverrideTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverrideMemberFromDerivedClass()
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
                    </Workspace>, renameTo:="A")

            End Using
        End Sub

        <WorkItem(25682, "https://github.com/dotnet/roslyn/issues/25682")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverrideMemberFromDerivedClassWhenMemberIsPrivate()
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
                    </Workspace>, renameTo:="A")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverrideMemberFromDerivedClass_abstract_virtual()
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
                    </Workspace>, renameTo:="A")

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOverrideMemberFromDerivedClass_abstract_override()
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
                    </Workspace>, renameTo:="A")

            End Using
        End Sub
    End Class
End Namespace
