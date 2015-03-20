' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    Public Class InheritedByGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub TestInheritedByClasses()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

interface IBlah {
}

abstract class $$Base
{
    public abstract int CompareTo(object obj);
}

class Foo : Base, IComparable, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}

class Foo2 : Base, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base)" Category="CodeSchema_Class" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="Base" Icon="Microsoft.VisualStudio.Class.Internal" Label="Base"/>
                            <Node Id="(@1 Type=Foo)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Foo" Icon="Microsoft.VisualStudio.Class.Internal" Label="Foo"/>
                            <Node Id="(@1 Type=Foo2)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Foo2" Icon="Microsoft.VisualStudio.Class.Internal" Label="Foo2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Foo)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                            <Link Source="(@1 Type=Foo2)" Target="(@1 Type=Base)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub TestInheritedByInterfaces()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

public interface $$I { }

interface I2 : I, IComparable
{
    void M();
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = testState.GetGraphContextAfterQuery(inputGraph, New InheritedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Public" Label="I"/>
                            <Node Id="(@1 Type=I2)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I2" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I2"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=I2)" Target="(@1 Type=I)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Sub
    End Class

End Namespace
