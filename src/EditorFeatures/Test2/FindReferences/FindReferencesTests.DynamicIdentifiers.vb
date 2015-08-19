' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalVariable()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Foo()
	            {
		            dynamic {|Definition:$$i|} = 0;
                    [|i|] = "foo";
                    [|i|] = new object();
		            Console.WriteLine([|i|]);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalObject()
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
	            void Foo()
	            {	
		            dynamic {|Definition:$$o|} = new B();
		            Console.WriteLine([|o|].i);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalLambda()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            delegate void myDelegate(dynamic d);
	            void Foo()
	            {	
                   int x = 10;
		           myDelegate del = {|Definition:n|} => { [|$$n|] = [|n|] % 5; Console.WriteLine([|n|]);};
                   del(x);
	            }
            }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalArray()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Foo()
	            {
		            dynamic[] {|Definition:$$x|} = new dynamic[10];
		            Console.WriteLine([|x|].Length);
		
	            }
            }        
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestLocalCast()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class A
            {
	            void Foo()
	            {
		            int i = 10;
		            dynamic {|Definition:j|} = i;
		            i = (int)[|$$j|];
	            }
            }       
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
