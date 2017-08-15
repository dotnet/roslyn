' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalVariable() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Goo()
	            {
		            dynamic {|Definition:$$i|} = 0;
                    [|i|] = "goo";
                    [|i|] = new object();
		            Console.WriteLine([|i|]);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalObject() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            class B
	            {
		            public int i;
	            }
	            void Goo()
	            {	
		            dynamic {|Definition:$$o|} = new B();
		            Console.WriteLine([|o|].i);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalLambda() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            delegate void myDelegate(dynamic d);
	            void Goo()
	            {	
                   int x = 10;
		           myDelegate del = {|Definition:n|} => { [|$$n|] = [|n|] % 5; Console.WriteLine([|n|]);};
                   del(x);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalArray() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Goo()
	            {
		            dynamic[] {|Definition:$$x|} = new dynamic[10];
		            Console.WriteLine([|x|].Length);
		
	            }
            }        
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLocalCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Goo()
	            {
		            int i = 10;
		            dynamic {|Definition:j|} = i;
		            i = (int)[|$$j|];
	            }
            }       
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input)
        End Function
    End Class
End Namespace
