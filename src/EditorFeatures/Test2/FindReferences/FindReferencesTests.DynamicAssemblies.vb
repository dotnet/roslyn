' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestLocalVariableOnDeclaration(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
        <Document>
            using System;
            namespace DynamicFARTest
            {
	            public class A
	            {
		            public dynamic {|Definition:$$d|};	
	            }
           }      
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
            using System;
            namespace DynamicFARTest
            {
	            class B
	            {
		            void Boo()
		            {
			            A a = new A();
			            a.[|d|] = 1;
                        Console.WriteLine(a.[|d|]);			           
		            }
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDynamicFunctionOnDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
        <Document>
            namespace DynamicFARTest
            {
	            public class A
	            {
		            public void {|Definition:$$Dynamic|}(int d){}
	            }
            }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
            namespace DynamicFARTest
            {
	            class B
	            {
		            void Aoo()
		            {
			            A a = new A();
                        dynamic d = 1;
			            a.[|Dynamic|](d);
		            }
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace

