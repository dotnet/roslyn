' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterInMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
            }

            void Bar()
            {
                Foo([|i|]: 0);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Foo(int {|Definition:$$i|})
            {
                Console.WriteLine([|i|]);
                Console.WriteLine(I);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameterCaseSensitivity2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            sub Foo(byval {|Definition:$$i|} as Integer)
                Console.WriteLine([|i|])
                Console.WriteLine([|I|])
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void foo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void foo(string {|Definition:name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542475")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPartialParameter2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class program
{
    static partial void foo(string {|Definition:name|}, int age, bool sex, int index1 = 1) { }
}
partial class program
{
    static partial void foo(string {|Definition:$$name|}, int age, bool sex, int index1 = 1);
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

#Region "FAR on partial methods"

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_CSharpWithSignaturesMatchFARParameterOnDefDecl() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParameter_VBWithSignaturesMatchFARParameterOnDefDecl() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C                
           partial sub PM(x as Integer, y as Integer)
              
           End Sub
           partial sub PM({|Definition:x|} as Integer, y as Integer)
                Dim y as Integer = [|$$x|];;
           End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

#End Region

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Foo(Sub({|Definition:$$x|} As Integer) Return, Sub({|Definition:x|} As Integer) Return)
    End Sub
 
    Sub Foo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(624310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624310")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter4() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(543276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543276")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
 
Module Program
    Sub Main
        Foo(Sub({|Definition:x|} As Integer) Return, Sub({|Definition:$$x|} As Integer) Return)
    End Sub
 
    Sub Foo(Of T)(x As T, y As T)
    End Sub
End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAnonymousFunctionParameter5() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter1() As Task
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
        Dim y = x.Foo(0, $$[|defaultValue|]:="")
    End Sub

    <Extension>
    Function Foo(x As Stack(Of String), index As Integer, {|Definition:defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(545654, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545654")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestReducedExtensionNamedParameter2() As Task
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
        Dim y = x.Foo(0, [|defaultValue|]:="")
    End Sub

    <Extension>
    Function Foo(x As Stack(Of String), index As Integer, {|Definition:$$defaultValue|} As String) As String
    End Function
End Module
        ]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter2() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharp_TestAnonymousMethodParameter4() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter2() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(545618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545618")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVB_TestAnonymousMethodParameter4() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace
