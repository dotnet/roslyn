' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicParameterCall() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicArgumentCall() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionCallWithKnownTypeReturn() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionCallWithDynamicTypeReturn() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicParameterOnDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionDynamicArgumentOnDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDoubleFunctionDynamicOnDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnKnownTypeDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnDynamicTypeDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnDynamicTypeCall() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionOverloadOnStaticTypeCall() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace


