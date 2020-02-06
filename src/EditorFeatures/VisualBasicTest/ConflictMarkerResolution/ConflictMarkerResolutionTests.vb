' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConflictMarkerResolution
    Public Class ConflictMarkerResolutionTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicResolveConflictMarkerCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)>
        Public Async Function TestTakeTop1() As Task
            Await TestInRegularAndScript1Async(
"
imports System

namespace N
[|<<<<<<<|] This is mine!
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
=======
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
>>>>>>> This is theirs!
end namespace",
"
imports System

namespace N
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
end namespace", index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)>
        Public Async Function TestTakeBottom1() As Task
            Await TestInRegularAndScript1Async(
"
imports System

namespace N
[|<<<<<<<|] This is mine!
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
=======
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
>>>>>>> This is theirs!
end namespace",
"
imports System

namespace N
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
end namespace", index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsResolveConflictMarker)>
        Public Async Function TestTakeBoth1() As Task
            Await TestInRegularAndScript1Async(
"
imports System

namespace N
[|<<<<<<<|] This is mine!
    class Program
        sub Main()
            dim p as Program
            Console.WriteLine(""My section"")
        end sub
    end class
=======
    class Program2
        sub Main2()
            dim p as Program2
            Console.WriteLine(""Their section"")
        end sub
    end class
>>>>>>> This is theirs!
end namespace",
"
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
end namespace", index:=2)
        End Function
    End Class
End Namespace
