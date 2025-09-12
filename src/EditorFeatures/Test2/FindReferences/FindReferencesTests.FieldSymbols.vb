' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539598")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539598")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540515")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/pull/4952")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestField_ValueUsageInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int {|Definition:$$i|};

            void Goo()
            {
                Console.WriteLine({|ValueUsageInfo.Read:[|i|]|});
                {|ValueUsageInfo.Write:[|i|]|} = 0;
                {|ValueUsageInfo.ReadWrite:[|i|]|}++;
                Goo2(in {|ValueUsageInfo.ReadableReference:[|i|]|}, ref {|ValueUsageInfo.ReadableWritableReference:[|i|]|});
                Goo3(out {|ValueUsageInfo.WritableReference:[|i|]|});
                Console.WriteLine(nameof({|ValueUsageInfo.Name:[|i|]|}));
            }

            void Goo2(in int j, ref int k)
            {
            }

            void Goo3(out int i)
            {
                i = 0;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestField_ContainingTypeInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int {|Definition:$$i|};

            int P
            {
                get
                {
                    return {|AdditionalProperty.ContainingTypeInfo.C:[|i|]|}
                }
            }

            int P2 => {|AdditionalProperty.ContainingTypeInfo.C:[|i|]|}

            void Goo()
            {
                Console.WriteLine({|AdditionalProperty.ContainingTypeInfo.C:[|i|]|});
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestField_ContainingMemberInfo(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            int {|Definition:$$i|};

            int P
            {
                get
                {
                    return {|AdditionalProperty.ContainingMemberInfo.P:[|i|]|}
                }
            }

            int P2 => {|AdditionalProperty.ContainingMemberInfo.P2:[|i|]|}

            void Goo()
            {
                Console.WriteLine({|AdditionalProperty.ContainingMemberInfo.Goo:[|i|]|});
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestFieldReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~F:C.[|i|]")]

        class C
        {
            int {|Definition:$$i|};
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestField_UsedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        public partial class C
        {
            public static int {|Definition:$$i|};

        }
        </Document>
        <DocumentFromSourceGenerator>
        public partial class C
        {
            void Goo()
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(new C().[|i|]);
            }
        }
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
