' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicParameterCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:Boo|}(dynamic d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            b.[|$$Boo|]("b");
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicArgumentCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		           public void {|Definition:Boo|}(int d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
                    dynamic d = 1;
		            b.[|$$Boo|](d);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionCallWithKnownTypeReturn(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public int Boo1(int d){ return d;}
		            public void {|Definition:Boo2|}(int x){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            b.[|$$Boo2|](b.Boo1((dynamic)1));
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionCallWithDynamicTypeReturn(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public dynamic Boo1(dynamic d){ return d;}
		            public void {|Definition:Boo2|}(int x){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            b.[|$$Boo2|](b.Boo1(1));
	            }
            }     
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicParameterOnDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:$$Boo|}(dynamic d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            b.[|Boo|]("b");
	            }
            }     
         </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicArgumentOnDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:$$Boo|}(int d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            dynamic d = 1;
		            b.[|Boo|](d);
	            }
            }      
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionDynamicOnDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public int Boo1(dynamic d){ return d;}
		            public void {|Definition:$$Boo2|}(int x){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            b.[|Boo2|](b.Boo1(1));
	            }
            }      
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnKnownTypeDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:$$Boo|}(int d){}
		            public void Boo(dynamic d){}
		            public void Boo(string d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            dynamic d = 1;
		            b.[|Boo|](d);
		            b.[|Boo|](1);
		            b.Boo("Hello");
	            }
            }      
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnDynamicTypeDefinition(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void Boo(int d){}
		            public void {|Definition:$$Boo|}(dynamic d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            dynamic d = 1;
		            b.[|Boo|](d);
		            b.Boo(1);
		            b.[|Boo|]("Hello");
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnDynamicTypeCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:Boo|}(int d){}
		            public void Boo(dynamic d){}
		            public void Boo(string d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            dynamic d = 1;
		            b.[|$$Boo|](d);
		            b.[|Boo|](1);
		            b.Boo("Hello");
	            }
            }   
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnStaticTypeCall(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public void {|Definition:Boo|}(int d){}
		            public void Boo(dynamic d){}
		            public void Boo(string d){}
	            }
	            void Aoo()
	            {
		            B b = new B();
		            dynamic d = 1;
		            b.[|Boo|](d);
		            b.[|$$Boo|](1);
		            b.Boo("Hello");
	            }
            }   
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace


