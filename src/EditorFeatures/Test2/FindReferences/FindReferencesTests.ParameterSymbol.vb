' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar(int i)
            {
                Console.WriteLine(i);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar()
            {
                Goo([|i|]: 0);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Goo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(I);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            sub Goo(byval {|Definition:$$i|} as Integer)
                Console.WriteLine([|i|])
                Console.WriteLine([|I|])
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void goo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void goo(string {|Definition:name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void goo(string {|Definition:name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void goo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#Region "FAR on partial methods"

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_CSharpWithSignaturesMatchFARParameterOnDefDecl(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        partial class C
        {          
            partial void PM(int {|Definition:$$x|}, int y);
            partial void PM(int {|Definition:x|}, int y)
            {
                int s = [|x|];
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_VBWithSignaturesMatchFARParameterOnDefDecl(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           partial sub PM({|Definition:x|} as Integer, y as Integer)
              
           End Sub

           sub PM({|Definition:x|} as Integer, y as Integer)
                Dim y as Integer = [|$$x|];;
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Goo(Sub({|Definition:$$x|} As Integer) Return, Sub({|Definition:x|} As Integer) Return)
    End Sub
 
    Sub Goo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    
    Dim field As Object = If(True, Function({|Definition:$$x|} As String) [|x|].ToUpper(), Function({|Definition:x|} As String) [|x|].ToLower())

End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class Program
{
    public object O = true ? (Func<string, string>)((string {|Definition:$$x|}) => {return [|x|].ToUpper(); }) : (Func<string, string>)((string x) => {return x.ToLower(); });
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Goo(Sub({|Definition:x|} As Integer) Return, Sub({|Definition:$$x|} As Integer) Return)
    End Sub
 
    Sub Goo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Module M
    Sub Main()
        Dim s = Sub({|Definition:$$x|}) Return
        s([|x|]:=1)
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim x As New Stack(Of String)
        Dim y = x.Goo(0, $$[|defaultValue|]:="")
    End Sub

    <Extension>
    Function Goo(x As Stack(Of String), index As Integer, {|Definition:defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim x As New Stack(Of String)
        Dim y = x.Goo(0, [|defaultValue|]:="")
    End Sub

    <Extension>
    Function Goo(x As Stack(Of String), index As Integer, {|Definition:$$defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = {|Definition:$$a|} => [|a|];
        Converter<int, int> c = a => a;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = {|Definition:a|} => [|$$a|];
        Converter<int, int> c = a => a;
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = a => a;
        Converter<int, int> c = {|Definition:$$a|} => [|a|];
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class X
{
    void Main()
    {
        Func<int, int> f = a => a;
        Converter<int, int> c = {|Definition:a|} => [|$$a|];
    }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function({|Definition:$$a|}) [|a|]
        dim c as Converter(of integer, integer) = Function(a) a
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function({|Definition:a|}) [|$$a|]
        dim c as Converter(of integer, integer) = Function(a) a
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function(a) a
        dim c as Converter(of integer, integer) = Function({|Definition:$$a|}) [|a|]
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

class X
    sub Main()
        dim f as Func(of integer, integer) = Function(a) a
        dim c as Converter(of integer, integer) = Function({|Definition:a|}) [|$$a|]
    end sub
end class
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:$$i|})
                {
                    Console.WriteLine([|i|]);
                }

                Local(1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:$$i|})
                {
                }

                Local([|i|]:1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:i|})
                {
                    Console.WriteLine([|$$i|]);
                }

                Local(1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)>
        Public Async Function TestParameterInLocalFunction4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Main()
            {
                void Local(int {|Definition:i|})
                {
                }

                Local([|$$i|]:1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
