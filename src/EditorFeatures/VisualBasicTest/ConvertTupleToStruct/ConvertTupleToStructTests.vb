' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertTupleToStruct
    Public Class ConvertTupleToStructTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(Workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertTupleToStructCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

#Region "update containing member tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleType() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType() As Task
            Dim text = "
class Test
{
    void Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2)
        (int a, int b) t2 = (a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType2() As Task
            Dim text = "
class Test
{
    (int a, int b) Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2)
        (int a, int b) t2 = (a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType3() As Task
            Dim text = "
class Test
{
    (int a, int b) Method()
    {
        [||](int a, int b) t1 = (a: 1, b: 2)
        (int b, int a) t2 = (b: 1, a: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType4() As Task
            Dim text = "
class Test
{
    void Method()
    {
        (int a, int b) t1 = (a: 1, b: 2)
        [||](int a, int b) t2 = (a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypeInNamespace() As Task
            Dim text = "
namespace N
{
    class Test
    {
        void Method()
            var t1 = [||](a: 1, b: 2)
        }
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestNonLiteralNames() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: Foo(), b: Bar())
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypeWithInferredName() As Task
            Dim text = "
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleInstancesInSameMethod() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
        var t2 = (a: 3, b: 4)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleInstancesAcrossMethods() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
        var t2 = (a: 3, b: 4)
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2)
        var t2 = (a: 3, b: 4)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function OnlyConvertMatchingTypesInSameMethod() As Task
            Dim text = "
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2)
        var t2 = (a: 3, b)
        var t3 = (a: 4, b: 5, c: 6)
        var t4 = (b: 5, a: 6)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestFixAllMatchesInSingleMethod() As Task
            Dim text = "
class Test
{
    void Method(int b)
    {
        var t1 = [||](a: 1, b: 2)
        var t2 = (a: 3, b)
        var t3 = (a: 4, b: 5, c: 6)
        var t4 = (b: 5, a: 6)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestFixNotAcrossMethods() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
        var t2 = (a: 3, b: 4)
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2)
        var t2 = (a: 3, b: 4)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestTrivia() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]( /*2*/ a /*3*/ : /*4*/ 1 /*5*/ , /*6*/ b /*7*/ = /*8*/ 2 /*9*/ ) /*10*/ 
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function NotIfReferencesAnonymousTypeInternally() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: new { c = 1, d = 2 })
    }
}
"

            Await TestMissingInRegularAndScriptAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod1() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: (object)(a: 1, b: default(object)))
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod2() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: (object)[||](a: 1, b: default(object)))
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function RenameAnnotationOnStartingPoint() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
        var t2 = [||](a: 3, b: 4)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function CapturedMethodTypeParameters() As Task
            Dim text = "
class Test<X> where X : struct
{
    void Method<Y>(List<X> x, Y[] y) where Y : class, new()
    {
        var t1 = [||](a: x, b: y)
    }
}
"
            Dim expected = ""

            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.and_update_usages_in_containing_member
            })
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function NewTypeNameCollision() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}

class NewStruct
{
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestDuplicatedName() As Task
            Dim text = "
class Test
{
    void Method()
    {
        var t1 = [||](a: 1, a: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLambda1() As Task
            Dim text = "
imports System

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
        Action a = () =>
            var t2 = (a: 3, b: 4)
        }
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLambda2() As Task
            Dim text = "
imports System

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
        Action a = () =>
            var t2 = [||](a: 3, b: 4)
        }
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLocalFunction1() As Task
            Dim text = "
imports System

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
        void Goo()
            var t2 = (a: 3, b: 4)
        }
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLocalFunction2() As Task
            Dim text = "
imports System

class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
        void Goo()
            var t2 = [||](a: 3, b: 4)
        }
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

#End Region

#Region "update containing type tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestCapturedTypeParameter_UpdateType() As Task
            Dim text = "
imports System

class Test<T>
{
    void Method(T t)
    {
        var t1 = [||](a: t, b: 2)
    }

    T t
    void Goo()
    {
        var t2 = (a: t, b: 4)
    }

    void Blah<T>(T t)
    {
        var t2 = (a: t, b: 4)
    }
}
"
            Dim expected = ""

            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.and_update_usages_in_containing_member,
                FeaturesResources.and_update_usages_in_containing_type
            })
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_SinglePart_SingleFile() As Task
            Dim text = "
imports System

class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }

    void Goo()
    {
        var t2 = (a: 3, b: 4)
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_MultiplePart_SingleFile() As Task
            Dim text = "
imports System

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4)
    }
}

class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}
"
            Dim expected = ""
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_MultiplePart_MultipleFile() As Task
            Dim text = "
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
        <Document>
imports System

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4)
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"

            Dim expected = """C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = new {|Rename:NewStruct|}(a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}

internal struct NewStruct
{
    public int a
    public int b

    public NewStruct(int a, int b)
    {
        this.a = a
        this.b = b
    }

    public override bool Equals(object obj)
    {
        if (!(obj is NewStruct))
            return false
        }

        var other = (NewStruct)obj
        return a == other.a &amp&amp
               b == other.b
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        return hashCode
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a
        b = this.b
    }

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b)
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b)
    }
}</Document>
        <Document>
imports System

partial class Test
{
    NewStruct Goo()
    {
        var t2 = new NewStruct(a: 3, b: 4)
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

#End Region

#Region "update containing project tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInProject_MultiplePart_MultipleFile_WithNamespace() As Task
            Dim text = "
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

namespace N
{
    partial class Test
    {
        void Method()
            var t1 = [||](a: 1, b: 2)
        }
    }

    partial class Other
    {
        void Method()
            var t1 = (a: 1, b: 2)
        }
    }
}
        </Document>
        <Document>
imports System

partial class Test
{
    (int a, int b) Goo()
    {
        var t2 = (a: 3, b: 4)
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"

            Dim expected = """C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

namespace N
{
    partial class Test
    {
        void Method()
            var t1 = new {|Rename:NewStruct|}(a: 1, b: 2)
        }
    }

    partial class Other
    {
        void Method()
            var t1 = new NewStruct(a: 1, b: 2)
        }
    }

    internal struct NewStruct
    {
        public int a
        public int b

        public NewStruct(int a, int b)
            this.a = a
            this.b = b
        }

        public override bool Equals(object obj)
            if (!(obj is NewStruct))
            {
                return false
            }

            var other = (NewStruct)obj
            return a == other.a &amp&amp
                   b == other.b
        }

        public override int GetHashCode()
            var hashCode = 2118541809
            hashCode = hashCode * -1521134295 + a.GetHashCode()
            hashCode = hashCode * -1521134295 + b.GetHashCode()
            return hashCode
        }

        public void Deconstruct(out int a, out int b)
            a = this.a
            b = this.b
        }

        public static implicit operator (int a, int b) (NewStruct value)
            return (value.a, value.b)
        }

        public static implicit operator NewStruct((int a, int b) value)
            return new NewStruct(value.a, value.b)
        }
    }
}
        </Document>
        <Document>
imports System

partial class Test
{
    N.NewStruct Goo()
    {
        var t2 = new N.NewStruct(a: 3, b: 4)
    }
}

partial class Other
{
    void Goo()
    {
        var t1 = new N.NewStruct(a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=2)
        End Function

#End Region

#Region "update dependent projects"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateDependentProjects_DirectDependency() As Task
            Dim text = "
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
imports System

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Dim expected = """C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2)
    }
}

public struct NewStruct
{
    public int a
    public int b

    public NewStruct(int a, int b)
    {
        this.a = a
        this.b = b
    }

    public override bool Equals(object obj)
    {
        if (!(obj is NewStruct))
            return false
        }

        var other = (NewStruct)obj
        return a == other.a &amp&amp
               b == other.b
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        return hashCode
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a
        b = this.b
    }

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b)
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b)
    }
}</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
imports System

partial class Other
{
    void Goo()
    {
        var t1 = new NewStruct(a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateDependentProjects_NoDependency() As Task
            Dim text = "
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = [||](a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
imports System

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Dim expected = """C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2)
    }
}

partial class Other
{
    void Method()
    {
        var t1 = new NewStruct(a: 1, b: 2)
    }
}

public struct NewStruct
{
    public int a
    public int b

    public NewStruct(int a, int b)
    {
        this.a = a
        this.b = b
    }

    public override bool Equals(object obj)
    {
        if (!(obj is NewStruct))
            return false
        }

        var other = (NewStruct)obj
        return a == other.a &amp&amp
               b == other.b
    }

    public override int GetHashCode()
    {
        var hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        return hashCode
    }

    public void Deconstruct(out int a, out int b)
    {
        a = this.a
        b = this.b
    }

    public static implicit operator (int a, int b) (NewStruct value)
    {
        return (value.a, value.b)
    }

    public static implicit operator NewStruct((int a, int b) value)
    {
        return new NewStruct(value.a, value.b)
    }
}</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
imports System

partial class Other
{
    void Goo()
    {
        var t1 = (a: 1, b: 2)
    }
}
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=3)
        End Function

#End Region
    End Class

End Namespace
