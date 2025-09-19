' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOnDynamicCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public B(){}
		            public {|Definition:B|}(int d){}
                    public B(string d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            B b = new [|$$B|](d);
	            }
            }   
       </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOnOverloadedDynamicDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public B(){}
		            public {|Definition:$$B|}(int d){}
                    public B(string d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            B b = new [|B|](d);
	            }
            }   
       </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOnTypeName(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class {|Definition:$$B|}
	            {
		            public {|Definition:B|}(){}
		            public {|Definition:B|}(int d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            [|B|] b1 = new [|B|]();
		            [|B|] b2 = new [|B|](d);
	            }
            }   
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOnNonDynamicCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public {|Definition:B|}(){}
		            public B(int d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            B b1 = new [|$$B|]();
		            B b2 = new B(d);
	            }
            }      
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOverloadedOnNonDynamicDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public {|Definition:$$B|}(){}
		            public B(int d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            B b1 = new [|B|]();
		            B b2 = new B(d);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorOverloadedOnDynamicTypeDeclaration(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class {|Definition:B|}
	            {
		            public {|Definition:B|}(){}
		            public {|Definition:B|}(int d){}
	            }
	            void Aoo()
	            {
		            dynamic d = 1;
		            [|B|] b1 = new [|B|]();
		            [|$$B|] b2 = new [|B|](d);
	            }
            }     
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace

