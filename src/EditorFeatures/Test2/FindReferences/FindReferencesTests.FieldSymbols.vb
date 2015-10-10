' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_Private_SameType()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int {|Definition:$$i|};

            void Foo()
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(new C().[|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_Private_WrappedInProperty()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_MultipleVariableDeclarators()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int i = 1, {|Definition:$$j|} = 2;

            void Foo()
            {
                Console.WriteLine(i);
                Console.WriteLine([|j|]);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_Public_OtherType()
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
            void Foo()
            {
                Console.WriteLine(j);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_Inaccessible()
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
            void Foo()
            {
                Console.WriteLine(j);
                Console.WriteLine(new C().[|j|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_InDependentProject1()
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
            sub Foo()
                Bar(new c().[|field|])
                Bar(new C().[|Field|])
                Bar(new C().blah)
            end sub
        end class
        </Document>
        <Document>
        class E
            sub Foo()
                ' Find, even in file without the original symbol name.
                Bar(new c().[|Field|])
                Bar(new C().blah)
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_CSharpInaccessibleStaticField()
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
            void Foo()
            {
                int j = C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_VBInaccessibleStaticField()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              private shared {|Definition:$$j|} As Integer = 2
        End Class

        Class D
              Sub Foo()
                  Dim j As Integer = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_CSharpInaccessibleStaticProtectedField()
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
            void Foo()
            {
                object j = C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_VBInaccessibleStaticProtectedField()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              protected shared {|Definition:$$j|} As Object = 2
        End Class

        Class D
              Sub Foo()
                  Dim j As Object = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_CSharpInaccessibleInstanceProtectedField()
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
            void Foo()
            {
                C.[|j|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_VBInaccessibleInstanceProtectedField()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              protected {|Definition:$$j|} As string = 2
        End Class

        Class D
              Sub Foo()
                  Dim j As string = C.[|j|]
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(539598)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_EnumMember1()
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
            Test(input)
        End Sub

        <WorkItem(539598)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_EnumMember2()
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
            Test(input)
        End Sub

        <WorkItem(540515)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_AcrossSubmission()
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
        object {|Definition:$$foo|};
    </Submission>
    <Submission Language="C#" CommonReferences="true">
        [|foo|]
    </Submission>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(4952, "https://github.com/dotnet/roslyn/pull/4952")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub Field_AcrossSubmission_Command()
            Dim input =
<Workspace>
    <Submission Language="C#" CommonReferences="true">
        object {|Definition:$$foo|};
    </Submission>
    <Submission Language="NoCompilation" CommonReferences="false">
        #help
    </Submission>
    <Submission Language="C#" CommonReferences="true">
        [|foo|]
    </Submission>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCrefField()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Definition:Program
{
    private int {|Definition:foo|};
    ///  <see cref="[|foo$$|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCrefField2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Definition:Program
{
    private int {|Definition:foo$$|};
    ///  <see cref="[|foo|]"/> to start the program.
    static void Main(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
