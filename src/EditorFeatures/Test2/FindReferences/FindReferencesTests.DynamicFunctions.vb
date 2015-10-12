' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionDynamicParameterCall()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionDynamicArgumentCall()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestDoubleFunctionCallWithKnownTypeReturn()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestDoubleFunctionCallWithDynamicTypeReturn()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionDynamicParameterOnDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionDynamicArgumentOnDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestDoubleFunctionDynamicOnDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionOverloadOnKnownTypeDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionOverloadOnDynamicTypeDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionOverloadOnDynamicTypeCall()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestFunctionOverloadOnStaticTypeCall()
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
            Test(input)
        End Sub
    End Class
End Namespace


