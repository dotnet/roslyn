' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateMethod
    Partial Public Class GenerateMethodCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New CodeAnalysis.CSharp.CodeFixes.GenerateMethod.GenerateMethodCodeFixProvider())
            Else
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod.GenerateParameterizedMemberCodeFixProvider())
            End If
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInstanceMethod_CSharpToVisualBasic() As System.Threading.Tasks.Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass v;
                            v.$$Bar();
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass

                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
public class VBClass
    Public Sub Bar()
        Throw New NotImplementedException()
    End Sub
end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleStaticMethod_CSharpToVisualBasic() As System.Threading.Tasks.Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass.$$Bar();
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass

                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
public class VBClass
    Public Shared Sub Bar()
        Throw New NotImplementedException()
    End Sub
end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParameters_CSharpToVisualBasic() As System.Threading.Tasks.Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass v;
                            int i;
                            char c;
                            double d;
                            string s;
                            bool b = v.$$Bar(i, ref c, out d, namedParam: s);
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass

                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
public class VBClass
    Public Function Bar(i As Integer, ByRef c As Char, ByRef d As Double, namedParam As String) As Boolean
        Throw New NotImplementedException()
    End Function
end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestExplicitInterface_CSharpToVisualBasic() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass : IVBInterface
                    {
                        bool IVBInterface.$$Foo(string s)
                        {
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public interface IVBInterface

                    end interface
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                    public interface IVBInterface
                        Function Foo(s As String) As Boolean
                    end interface
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDelegate_CSharpToVisualBasic() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document><![CDATA[
                    using System;
                    public class CSClass
                    {
                        void Foo()
                        {
                            Bar(VBClass.$$Method)
                        }

                        void Bar(Func<int,string> f)
                        {
                        }
                    }]]>
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass

                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                    public class VBClass
                        Public Shared Function Method(arg As Integer) As String
                            Throw New NotImplementedException()
                        End Function
                    end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestAbstractMethod_CSharpToVisualBasic() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass v;
                            v.$$Bar();
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public mustinherit class VBClass

                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                    public mustinherit class VBClass
                        Public MustOverride Sub Bar()
                    end class
                </text>.Value.Trim()

            Await TestAsync(input, expected, codeActionIndex:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInstanceMethod_VisualBasicToCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
                    public class VBClass
                        public sub Foo()
                            dim v as CSClass
                            v.$$Bar()
                        end sub
                    end class
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class CSClass
                    {
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>using System;

                    public class CSClass
                    {
                        public void Bar()
                        {
                            throw new NotImplementedException();
                        }
                    }
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestIntoNestedType_CSharpToVisualBasic() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass.Inner v;
                            v.$$Bar();
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass
                        public class Inner
                        end class
                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>                    
                    public class VBClass
                        public class Inner
                            Public Sub Bar()
                                Throw New NotImplementedException()
                            End Sub
                        end class
                    end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestIntoNestedGenericType_CSharpToVisualBasic() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document><![CDATA[
                    public class CSClass
                    {
                        public void Foo()
                        {
                            VBClass<string>.Inner<int> v;
                            v.$$Bar();
                        }
                    }]]>
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class VBClass(of X)
                        public class Inner(of Y)
                        end class
                    end class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
    public class VBClass(of X)
        public class Inner(of Y)
            Public Sub Bar()
                Throw New NotImplementedException()
            End Sub
        end class
    end class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestIntoNestedType_VisualBasicToCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
                    public class VBClass
                        public sub Foo()
                            dim v as CSClass.Inner
                            v.$$Bar()
                        end sub
                    end class
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class CSClass
                    {
                        public class Inner
                        {
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>using System;

                    public class CSClass
                    {
                        public class Inner
                        {
                            public void Bar()
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestIntoNestedGenericType_VisualBasicToCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
                    public class VBClass
                        public sub Foo()
                            dim v as CSClass(of Integer).Inner(of String)
                            v.$$Bar()
                        end sub
                    end class
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
                    public class CSClass<T>
                    {
                        public class Inner<U>
                        {
                        }
                    }]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[using System;

                    public class CSClass<T>
                    {
                        public class Inner<U>
                        {
                            public void Bar()
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_SingleNamedType() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
 
public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : T
    {
    }
}
 
public struct MyStruct<T>
{
 
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of String)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_2BaseTypeConstraints() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : AAA, BBB
    {
    }
}

public struct MyStruct<T>
{

}

public class AAA
{

}

public interface BBB
{

}

public class CCC : AAA, BBB
{

}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of CCC)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_2BaseTypeConstraints_Interfaces() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : AAA, BBB
    {
    }
}

public struct MyStruct<T>
{

}

public interface AAA
{

}

public interface BBB
{

}

public class CCC : AAA, BBB
{

}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of CCC)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_3BaseTypeConstraints_NoCommonDerived() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : interface1, interface2, interface3
    {
    }
}

public struct MyStruct<T>
{

}

public interface interface1
{

}

public interface interface2
{

}

public class derived1 : interface1, interface2
{

}

public interface interface3
{

}

public class derived2 : interface3
{

}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of Object)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        Public Async Function GenerateMethodUsingTypeConstraint_3BaseTypeConstraints_CommonDerived() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : interface1, interface2, interface3
    {
    }
}

public struct MyStruct<T>
{

}

public interface interface1
{

}

public interface interface2
{

}

public class derived1 : interface1, interface2
{

}

public interface interface3
{

}

public class derived2 : interface3
{

}

public class outer
{
    public class inner
    {
        public class derived3 : derived1, interface3
        {
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of outer.inner.derived3)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_3BaseTypeConstraints_CommonDerivedNestedType() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : interface1, interface2, interface3
    {
    }
}

public struct MyStruct<T>
{

}

public interface interface1
{

}

public interface interface2
{

}

public class derived1 : interface1, interface2
{

}

public interface interface3
{

}

public class derived2 : interface3
{

}

public class derived3 : derived1, interface3
{

}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of derived3)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_3BaseTypeConstraints_CommonDerivedInstantiatedTypes() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : interface1, interface2, interface3
    {
    }
}

public struct MyStruct<T>
{

}

public interface interface1
{

}

public interface interface2
{

}



public class derived1 : interface1, interface2
{

}

public interface interface3
{

}

public class derived2 : interface3
{

}

public class outer
{
    public class inner
    {
        public class derived3 : derived1, interface3
        {
        }
    }
}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of outer.inner.derived3)
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608827)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function GenerateMethodUsingTypeConstraint_InstantiatedGenerics() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange($$foo())
    End Sub
End Module</Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class extensions
{
    public static void AddRange<T, U>(this List<T> list, MyStruct<U> items) where U : Base1<AAA>, inter1
    {
    }
}

public struct MyStruct<T>
{

}

public interface inter1
{

}

public class Base1<T>
{

}

public class AAA
{

}

public class FinalType<T> : Base1<T>, inter1 where T : AAA
{

}]]>
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim list = New List(Of String)
        list.AddRange(foo())
    End Sub

    Private Function foo() As MyStruct(Of FinalType(Of AAA))
        Throw New NotImplementedException()
    End Function
End Module]]>
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

#Region "Normal tests"

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function PreferNormalFileOverAutoGeneratedFile_CSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Form1.cs">
class Form1
{
    void M() 
    { 
        UserControl1 control;
        control.Draw$$();
    }
}
        </Document>
        <Document FilePath="UserControl1.Designer.cs">
// This file is auto-generated
partial class UserControl1
{
    
}
        </Document>
        <Document FilePath="UserControl1.cs">
public partial class UserControl1
{
    
}
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"UserControl1.cs",
<Text>
using System;
public partial class UserControl1
{
    internal void Draw()
    {
        throw new NotImplementedException();
    }
}
</Text>.Value.Trim()},
                    {"UserControl1.Designer.cs",
<Text>
// This file is auto-generated
partial class UserControl1
{
    
}
</Text>.Value.Trim()}
                }

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function IntoAutoGeneratedFileIfNoBetterLocationExists_CSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Form1.cs">
class Form1
{
    void M() 
    { 
        UserControl1 control;
        control.Draw$$();
    }
}
        </Document>
        <Document FilePath="UserControl1.Designer.cs">
// This file is auto-generated
partial class UserControl1
{
    
}
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"UserControl1.Designer.cs",
<Text>
using System;
// This file is auto-generated
partial class UserControl1
{
    internal void Draw()
    {
        throw new NotImplementedException();
    }
}
</Text>.Value.Trim()}}

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function InAutoGeneratedFiles_CSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Form1.Designer.cs">
using System;
// This file is auto-generated
class Form1
{
    void M() 
    { 
        this.Draw$$();
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"Form1.Designer.cs",
<Text>
using System;
// This file is auto-generated
class Form1
{
    void M()
    {
        this.Draw();
    }

    private void Draw()
    {
        throw new NotImplementedException();
    }
}
</Text>.Value.Trim()}}

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function PreferNormalFileOverAutoGeneratedFile_Basic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document FilePath="Form1.vb">
Class Form1
    Sub M() 
        Dim control As UserControl1
        control.Draw$$()
    End Sub
End Class
        </Document>
        <Document FilePath="UserControl1.Designer.vb">
' This file is auto-generated
Partial Class UserControl1
    
End Class
        </Document>
        <Document FilePath="UserControl1.vb">
Partial Public Class UserControl1
    
End Class
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"UserControl1.vb",
<Text>
Partial Public Class UserControl1
    Friend Sub Draw()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Trim()},
                    {"UserControl1.Designer.vb",
<Text>
' This file is auto-generated
Partial Class UserControl1
    
End Class
</Text>.Value.Trim()}
                }

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function IntoAutoGeneratedFileIfNoBetterLocationExists_Basic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document FilePath="Form1.vb">
Class Form1
    Sub M() 
        Dim control As UserControl1
        control.Draw$$()
    End Sub
End Class
        </Document>
        <Document FilePath="UserControl1.Designer.vb">
' This file is auto-generated
Partial Class UserControl1
    
End Class
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"UserControl1.Designer.vb",
<Text>
' This file is auto-generated
Partial Class UserControl1
    Friend Sub Draw()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Trim()}}

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

        <WorkItem(144843, "Generate method stub generates into *.Designer.cs")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function InAutoGeneratedFiles_Basic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document FilePath="Form1.Designer.vb">
Class Form1
    Sub M() 
        Me.Draw$$()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expectedFileWithText =
                 New Dictionary(Of String, String) From {
                    {"Form1.Designer.vb",
<Text>
Class Form1
    Sub M() 
        Me.Draw()
    End Sub
    Private Sub Draw()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Trim()}}

            Await TestAsync(input, fileNameToExpected:=expectedFileWithText)
        End Function

#End Region

    End Class
End Namespace
