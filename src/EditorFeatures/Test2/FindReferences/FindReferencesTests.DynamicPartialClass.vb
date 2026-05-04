' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestDynamicLocalVariableOnDeclaration(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDynamicLocalVariableOnUse(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestDynamicFunction(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace

