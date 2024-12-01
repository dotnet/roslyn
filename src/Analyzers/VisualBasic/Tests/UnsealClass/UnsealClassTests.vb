' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UnsealClass

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UnsealClass
    <Trait(Traits.Feature, Traits.Features.CodeActionsUnsealClass)>
    Public NotInheritable Class UnsealClassTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicUnsealClassCodeFixProvider())
        End Function

        <Fact>
        Public Async Function RemovedFromSealedClass() As Task
            Await TestInRegularAndScriptAsync("
notinheritable class C
end class
class D
    inherits [|C|]
end class", "
class C
end class
class D
    inherits C
end class")
        End Function

        <Fact>
        Public Async Function RemovedFromSealedClassWithOtherModifiersPreserved() As Task
            Await TestInRegularAndScriptAsync("
public notinheritable class C
end class
class D
    inherits [|C|]
end class", "
public class C
end class
class D
    inherits C
end class")
        End Function

        <Fact>
        Public Async Function RemovedFromSealedClassWithConstructedGeneric() As Task
            Await TestInRegularAndScriptAsync("
notinheritable class C(of T)
end class
class D
    inherits [|C(of integer)|]
end class", "
class C(of T)
end class
class D
    inherits C(of integer)
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForNonSealedClass() As Task
            Await TestMissingInRegularAndScriptAsync("
class C
end class
class D
    inherits [|C|]
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForModule() As Task
            Await TestMissingInRegularAndScriptAsync("
module C
end module
class D
    inherits [|C|]
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForStruct() As Task
            Await TestMissingInRegularAndScriptAsync("
structure S
end structure
class D
    inherits [|S|]
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForDelegate() As Task
            Await TestMissingInRegularAndScriptAsync("
delegate sub F()
class D
    inherits [|F|]
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForSealedClassFromMetadata1() As Task
            Await TestMissingInRegularAndScriptAsync("
class D
    inherits [|string|]
end class")
        End Function

        <Fact>
        Public Async Function NotOfferedForSealedClassFromMetadata2() As Task
            Await TestMissingInRegularAndScriptAsync("
class D
    inherits [|System.ApplicationId|]
end class")
        End Function

        <Fact>
        Public Async Function RemovedFromAllPartialClassDeclarationsInSameFile() As Task
            Await TestInRegularAndScriptAsync("
partial public notinheritable class C
end class
partial class C
end class
partial notinheritable class C
end class
class D
    inherits [|C|]
end class", "
partial public class C
end class
partial class C
end class
partial class C
end class
class D
    inherits [|C|]
end class")
        End Function

        <Fact>
        Public Async Function RemovedFromAllPartialClassDeclarationsAcrossFiles() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
partial public notinheritable class C
end class
        </Document>
        <Document>
partial class C
end class
partial notinheritable class C
end class
        </Document>
        <Document>
class D
    inherits [|C|]
end class
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
partial public class C
end class
        </Document>
        <Document>
partial class C
end class
partial class C
end class
        </Document>
        <Document>
class D
    inherits C
end class
        </Document>
    </Project>
</Workspace>.ToString())
        End Function

        <Fact>
        Public Async Function RemovedFromClassInCSharpProject() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Project1">
        <ProjectReference>Project2</ProjectReference>
        <Document>
class D
    inherits [|C|]
end class
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="Project2">
        <Document>
public sealed class C
{
}
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Project1">
        <ProjectReference>Project2</ProjectReference>
        <Document>
class D
    inherits C
end class
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="Project2">
        <Document>
public class C
{
}
        </Document>
    </Project>
</Workspace>.ToString())
        End Function
    End Class
End Namespace
