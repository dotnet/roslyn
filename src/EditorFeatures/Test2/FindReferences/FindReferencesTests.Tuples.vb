' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests

        Dim tuple2 As XCData =
        <![CDATA[
namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}
]]>

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuples01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Alice|]|}:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|$$Alice|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuples02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|$$Alice|]|}:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuples03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]:1, Bob: 2);
            var y = ({|Definition:[|$$Alice|]|}:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuplesMultidocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program1
    {
        static void Main(string[] args)
        {
            // this is odd that these are considered references, but that is 
            // how it works for now.
            // NOTE: types like (int Alice, int Bob) may have distinct declaring references,
            //       but are also equal, in the symbol equality sense, types.
            var x = ([|Alice|]:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]:1, Bob: 2);
            var y = ({|Definition:[|$$Alice|]|}:1, Bob: 2);

            var z = x.[|Alice|];
        }
    }

        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldDifferentTuples01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Alice|]|}:1, Bob: 2);
            var y = (Alice:1.1, Bob: 2);

            var z = x.[|$$Alice|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuplesMatchOuterSymbols01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:[|Program|]|}:1, Main: 2);
            var y = ([|Program|]:1, Main: 2);

            var z = x.[|$$Program|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldSameTuplesMatchOuterSymbols02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = (1,2,3,4,5,6,7,8,9,10,11,{|Definition:[|Program|]|}:1, Main: 2);
            var x1 = (1,2,3,4,5,6,7,8,9,10,11,[|Program|]:1, Main: 2);
            var y = (Program:1, Main: 2);

            var z = x.[|$$Program|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldItem01(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:Alice|}:1, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldItem02(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ($$[|{|Definition:Alice|}|]:1, Bob: 2);
            var y = ([|Alice|]:1, Bob: 2);

            var z = x.Item1;
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldItem03(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            var x = ({|Definition:1|}, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
            var z1 = x.[|Item1|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTupleFieldItem04(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[

    class Program
    {
        static void Main(string[] args)
        {
            System.ValueTuple<int, int> x = (1, Bob: 2);
            var y = (Alice:1, Bob: 2);

            var z = x.$$[|Item1|];
            var z1 = x.[|Item1|];
        }
    }

        ]]>

            <%= tuple2 %>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
