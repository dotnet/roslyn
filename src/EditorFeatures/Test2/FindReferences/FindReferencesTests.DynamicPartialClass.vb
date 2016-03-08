' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDynamicLocalVariableOnDeclaration() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public partial class A
            {
	            dynamic {|Definition:$$d|};

            }      
        </Document>
        <Document>
            public partial class A
            {           
	            public dynamic D
	            {
		            get{ return [|d|];}
		            set{ [|d|] = value;}
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDynamicLocalVariableOnUse() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public partial class A
            {
	            dynamic {|Definition:d|};
	
            } 
        </Document>
        <Document>
            public partial class A
            {
	            public dynamic D
	            {
		            get{ return [|d|];}
		            set{ [|$$d|] = value;}
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDynamicFunction() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            namespace DynamicFARTest
            {
                public partial class A
                {
	                void {|Definition:$$Dynamic|}(int x){}
                }
            }  
        </Document>
        <Document>
            namespace DynamicFARTest
            {
                public partial class A
                {
	                void Aoo()
	                {
		                dynamic d = 1;
		                [|Dynamic|](d);
	                }
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace

