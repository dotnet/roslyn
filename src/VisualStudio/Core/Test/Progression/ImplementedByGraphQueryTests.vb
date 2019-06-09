' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    <[UseExportProvider]>
    Public Class ImplementedByGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestImplementedBy1() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
using System;

interface $$IBlah {
}

abstract class Base
{
    public abstract int CompareTo(object obj);
}

class Goo : Base, IComparable, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}

class Goo2 : Base, IBlah
{
    public override int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ImplementedByGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Goo)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo"/>
                            <Node Id="(@1 Type=Goo2)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Goo2" Icon="Microsoft.VisualStudio.Class.Internal" Label="Goo2"/>
                            <Node Id="(@1 Type=IBlah)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="IBlah" Icon="Microsoft.VisualStudio.Interface.Internal" Label="IBlah"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=Goo)" Target="(@1 Type=IBlah)" Category="Implements"/>
                            <Link Source="(@1 Type=Goo2)" Target="(@1 Type=IBlah)" Category="Implements"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class

End Namespace
