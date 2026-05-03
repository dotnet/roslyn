' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2040")>
        Public Async Function TestCRefTypeParameter1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    /// <summary>
    /// <see cref="S{$${|Definition:U|}}.Foo([|U|]?, S{[|U|]}?)"/>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
    }

    struct S<T> where T : struct
    {
        void Foo(T? x, S<T>? y)
        {
}
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2040")>
        Public Async Function TestCRefTypeParameter2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    /// <summary>
    /// <see cref="S{{|Definition:U|}}.Foo([|$$U|]?, S{[|U|]}?)"/>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
    }

    struct S<T> where T : struct
    {
        void Foo(T? x, S<T>? y)
        {
}
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
