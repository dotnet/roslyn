' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    Public Class OverridesGraphQueryTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestOverridesMethod1() As Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int $$CompareTo(object obj);
}

class Foo : Base, IComparable
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
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverridesGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                            <Node Id="(@1 Type=Foo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="CompareTo"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Foo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Target="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestOverridesMethod2() As Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int CompareTo(object obj);
}

class Foo : Base, IComparable
{
    public override int $$CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverridesGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Foo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="CompareTo"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
