' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddAnonymousTypeMemberName

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddAnonymousTypeMemberName
    Public Class AddAnonymousTypeMemberNameTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddAnonymousTypeMemberNameCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function Test1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    sub M()
        dim v = new with {[||]me.Equals(1)}
    end sub
end class",
"
class C
    sub M()
        dim v = new with {.{|Rename:V|} = me.Equals(1)}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function TestExistingName1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    sub M()
        dim v = new with {.V = 1, [||]me.Equals(1)}
    end sub
end class",
"
class C
    sub M()
        dim v = new with {.V = 1, .{|Rename:V1|} = me.Equals(1)}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function TestExistingName2() As Task
            Await TestInRegularAndScript1Async(
"
class C
    sub M()
        dim v = new with {.v = 1, [||]me.Equals(1)}
    end sub
end class",
"
class C
    sub M()
        dim v = new with {.v = 1, .{|Rename:V1|} = me.Equals(1)}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScript1Async(
"
class C
    sub M()
        dim v = new with {{|FixAllInDocument:|}new with {me.Equals(1), me.ToString() + 1}}
    end sub
end class",
"
class C
    sub M()
        dim v = new with {.P = new with {.V = me.Equals(1), .V1 = me.ToString() + 1}}
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScript1Async(
"
class C
{
    sub M()
    {
        dim v = new with {new with {{|FixAllInDocument:|}me.Equals(1), me.ToString() + 1}}
    }
end class",
"
class C
{
    sub M()
    {
        dim v = new with {.P = new with {.V = me.Equals(1), .V1 = me.ToString() + 1}}
    }
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)>
        Public Async Function TestFixAll3() As Task
            Await TestInRegularAndScript1Async(
"
class C
    sub M()
        dim v = new with {{|FixAllInDocument:|}new with {me.Equals(1), me.Equals(2)}}
    end sub
end class",
"
class C
    sub M()
        dim v = new with {.P = new with {.V = me.Equals(1), .V1 = me.Equals(2)}}
    end sub
end class")
        End Function
    End Class
End Namespace
