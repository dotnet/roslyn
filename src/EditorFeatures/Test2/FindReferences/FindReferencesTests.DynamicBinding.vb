' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionBindOnDefinition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
                {
                    public void {|Definition:$$Boo|}(int d) { }
                    class B
                    {
                        public void Aoo()
                        {
                            dynamic d = new A();
                            d.Boo();
                        }
                    }
                }     
       </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionBindOnUse() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            void Boo(){}
	            }
	            void Aoo()
	            {
		            dynamic d = new B();
		            d.$$Boo();
	            }
            }    
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPropertyBindOnUse() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            int i;
		            public I 
		            {
			            get { return i;}
			            set { i = value;}
		            }
	            }
	            void Aoo()
	            {
		            dynamic d = new B();
		            d.$$I = 1;
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPropertyBindOnDefinition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            int i;
		            public $$I
		            {
			            get { return i;}
			            set { i = value;}
		            }
	            }
	            void Aoo()
	            {
		            dynamic d = new B();
		            d.I = 1;
	            }
            }        
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function
    End Class
End Namespace

