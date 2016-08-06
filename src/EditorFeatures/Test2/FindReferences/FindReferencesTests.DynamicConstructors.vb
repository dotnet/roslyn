' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOnDynamicCall() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOnOverloadedDynamicDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOnTypeName() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOnNonDynamicCall() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOverloadedOnNonDynamicDefinition() As Task
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
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorOverloadedOnDynamicTypeDeclaration() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace

