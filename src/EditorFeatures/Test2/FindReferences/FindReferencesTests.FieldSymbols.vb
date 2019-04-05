' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_Private_SameType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int {|Definition:$$i|};

            void Goo()
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(new C().[|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_Private_WrappedInProperty(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class Test
        {
            private int {|Definition:myVar|} = 5;
            public int MyProperty
            {
                get { return [|myVar|]; }
                set { [|$$myVar|] = value; }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_MultipleVariableDeclarators(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int i = 1, {|Definition:$$j|} = 2;

            void Goo()
            {
                Console.WriteLine(i);
                Console.WriteLine([|j|]);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_Public_OtherType(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public int i = 1, {|Definition:$$j|} = 2;
        }

        class D
        {
            void Goo()
            {
                Console.WriteLine(j);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_Inaccessible(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int i = 1, {|Definition:$$j|} = 2;
        }

        class D
        {
            void Goo()
            {
                Console.WriteLine(j);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_InDependentProject1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        public class C
        {
            public int {|Definition:fie$$ld|};
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
        class D
            sub Goo()
                Bar(new c().[|field|])
                Bar(new C().[|Field|])
                Bar(new C().blah)
            end sub
        end class
        </Document>
        <Document>
        class E
            sub Goo()
                ' Find, even in file without the original symbol name.
                Bar(new c().[|Field|])
                Bar(new C().blah)
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleStaticField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           private static int {|Definition:$$j|} = 2;
        }

        class D
        {
            void Goo()
            {
                int j = C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleStaticField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              private shared {|Definition:$$j|} As Integer = 2
        End Class

        Class D
              Sub Goo()
                  Dim j As Integer = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleStaticProtectedField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected static object {|Definition:$$j|} = 2;
        }

        class D
        {
            void Goo()
            {
                object j = C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleStaticProtectedField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              protected shared {|Definition:$$j|} As Object = 2
        End Class

        Class D
              Sub Goo()
                  Dim j As Object = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstanceProtectedField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected string {|Definition:$$j|} = 2;
        }

        class D
        {
            void Goo()
            {
                C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstanceProtectedField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              protected {|Definition:$$j|} As string = 2
        End Class

        Class D
              Sub Goo()
                  Dim j As string = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539598")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_EnumMember1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class Program
{
    enum Days { Sat = 1, {|Definition:$$Sun|}, Mon, Tue, Wed, Thu, Fri };
    static void Main(string[] args)
    {
        int x = (int)Days.[|Sun|];
        Console.WriteLine("Sun = {0}", x);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(539598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539598")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_EnumMember2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class Program
{
    enum Days { Sat = 1, {|Definition:Sun|}, Mon, Tue, Wed, Thu, Fri };
    static void Main(string[] args)
    {
        int x = (int)Days.[|$$Sun|];
        Console.WriteLine("Sun = {0}", x);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(540515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540515")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_AcrossSubmission(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
        object {|Definition:$$goo|};
    </Submission>
    <Submission Language="C#" CommonReferences="true">
        [|goo|]
    </Submission>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(4952, "https://github.com/dotnet/roslyn/pull/4952")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_AcrossSubmission_Command(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
        object {|Definition:$$goo|};
    </Submission>
    <Submission Language="NoCompilation" CommonReferences="false">
        #help
    </Submission>
    <Submission Language="C#" CommonReferences="true">
        [|goo|]
    </Submission>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefField(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Definition:Program
{
    private int {|Definition:goo|};
    ///  <see cref="[|goo$$|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefField2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Definition:Program
{
    private int {|Definition:goo$$|};
    ///  <see cref="[|goo|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
