' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class OverridesGraphQueryTests
        <WpfFact>
        Public Async Function TestOverridesMethod1() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int $$CompareTo(object obj);
}

class Goo : Base, IComparable
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverridesGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" Label="CompareTo"/>
                            <Node Id="(@1 Type=Goo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="CompareTo"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Goo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Target="(@1 Type=Base Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestOverridesMethod2() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

abstract class Base
{
    public abstract int CompareTo(object obj);
}

class Goo : Base, IComparable
{
    public override int $$CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New OverridesGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Goo Member=(Name=CompareTo OverloadingParameters=[(@2 Namespace=System Type=Object)]))" Category="CodeSchema_Method" CodeSchemaProperty_IsPublic="True" CommonLabel="CompareTo" Icon="Microsoft.VisualStudio.Method.Public" IsOverloaded="True" Label="CompareTo"/>
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
