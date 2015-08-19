' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact(), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalVariableOnDeclaration()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestDynamicFunctionOnDefinition()
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
            Test(input)
        End Sub
    End Class
End Namespace

