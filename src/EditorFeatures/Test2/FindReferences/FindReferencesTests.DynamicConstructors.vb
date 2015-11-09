' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOnDynamicCall()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOnOverloadedDynamicDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOnTypeName()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOnNonDynamicCall()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOverloadedOnNonDynamicDefinition()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructorOverloadedOnDynamicTypeDeclaration()
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
            Test(input)
        End Sub
    End Class
End Namespace

