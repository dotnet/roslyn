' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
#Region "C#"

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionPredefinedType_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$int|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]int)new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionNamedType1_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$Int32|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]int)new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionNamedType2_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$int|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]Int32)new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionQualifiedNamedType1_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$System.Int32|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]int)new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionQualifiedNamedType2_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$int|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]System.Int32)new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionArrayType_CSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

struct Goo
{
    public static explicit operator {|Definition:$$int[]|}(Goo value) => default;

    static void M()
    {
        _ = [|(|]int[])new Goo();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region

#Region "Visual Basic"

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionPredefinedType_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as integer
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), integer)
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionNamedType1_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as int32
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), integer)
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionNamedType2_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as integer)
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), int32)
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionQualifiedNamedType1_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as system.int32
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), integer)
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionQualifiedNamedType2_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as integer
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), system.int32)
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/50850")>
        Public Async Function TestExplicitConversionArrayType_VisualBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
imports system

structure Goo
    Public Shared Narrowing Operator {|Definition:$$CType|}(value as goo) as integer()
        return nothing
    end operator

    sub M()
        dim y = [|ctype|](new Goo(), integer())
    end sub
end structure
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#End Region
    End Class
End Namespace
