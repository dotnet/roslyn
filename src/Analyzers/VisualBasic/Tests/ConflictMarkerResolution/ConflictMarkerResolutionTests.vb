' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ConflictMarkerResolution
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution.VisualBasicResolveConflictMarkerCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConflictMarkerResolution
    <Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)>
    Public Class ConflictMarkerResolutionTests

        <Fact>
        Public Async Function TestTakeTop1() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 0,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTakeBottom1() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 1,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTakeBoth1() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 2,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey
            }.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")>
        Public Async Function TestFixAll1() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
    end class

    class Program3
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 0,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey
            }.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")>
        Public Async Function TestFixAll2() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program2
    end class

    class Program4
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 1,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey
            }.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21107")>
        Public Async Function TestFixAll3() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
    end class
    class Program2
    end class

    class Program3
    end class
    class Program4
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 2,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTakeTop1_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:||||||| Baseline|}
    class Removed
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 0,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTakeBottom1_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:||||||| Baseline|}
    class Removed
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 1,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTakeBoth1_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
{|BC37284:||||||| Baseline|}
    class Removed
    end class
{|BC37284:=======|}
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 1,
                .CodeActionIndex = 2,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestFixAll1_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:||||||| Baseline|}
    class Removed 
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:||||||| Baseline|}
    class Removed2
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
    end class

    class Program3
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 0,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeTopEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestFixAll2_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:||||||||}
    class Removed
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:||||||||}
    class Removed2
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program2
    end class

    class Program4
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 1,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBottomEquivalenceKey
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestFixAll3_WithBaseline() As Task
            Dim source =
"
imports System

namespace N
{|BC37284:<<<<<<< This is mine!|}
    class Program
    end class
{|BC37284:||||||| Baseline|}
    class Removed
    end class
{|BC37284:=======|}
    class Program2
    end class
{|BC37284:>>>>>>> This is theirs!|}

{|BC37284:<<<<<<< This is mine!|}
    class Program3
    end class
{|BC37284:||||||| Baseline|}
    class Removed2
    end class
{|BC37284:=======|}
    class Program4
    end class
{|BC37284:>>>>>>> This is theirs!|}
end namespace"
            Dim fixedSource = "
imports System

namespace N
    class Program
    end class
    class Program2
    end class

    class Program3
    end class
    class Program4
    end class
end namespace"

            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource,
                .NumberOfIncrementalIterations = 2,
                .CodeActionIndex = 2,
                .CodeActionEquivalenceKey = AbstractResolveConflictMarkerCodeFixProvider.TakeBothEquivalenceKey
            }.RunAsync()
        End Function
    End Class
End Namespace
