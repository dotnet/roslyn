﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddObsoleteAttribute

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddObsoleteAttribute
    Public Class AddObsoleteAttributeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddObsoleteAttributeCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassNoMessage() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
end class

class Derived
    inherits [||]Base
end class
",
"
<System.Obsolete>
class Base
end class

<System.Obsolete>
class Derived
    inherits Base
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassWithMessage() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete(""message"")>
class Base
end class

class Derived
    inherits [||]Base
end class
",
"
<System.Obsolete(""message"")>
class Base
end class

<System.Obsolete>
class Derived
    inherits Base
end class
")
        End function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassUsedInField() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    dim i = [||]Base.i
end class
",
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    <System.Obsolete>
    dim i = Base.i
end class
")
        End function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassUsedInMethod() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    sub Goo()
        dim i = [||]Base.i
    end sub
end class
",
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    <System.Obsolete>
    sub Goo()
        dim i = Base.i
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteOverride() As Task
            ' VB gives no error here.
            Await TestMissingAsync(
"
class Base
    <System.Obsolete>
    protected overridable sub ObMethod()
    end sub
end class

class Derived
    inherits Base

    protected overrides sub [||]ObMethod()
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassFixAll1() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    sub Goo()
        dim i = {|FixAllInDocument:|}Base.i
        dim j = Base.i
    end sub
end class
",
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    <System.Obsolete>
    sub Goo()
        dim i = Base.i
        dim j = Base.i
    end sub
end class
")
        End function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassFixAll2() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    sub Goo()
        dim i = Base.i
        dim j = {|FixAllInDocument:|}Base.i
    end sub
end class
",
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    <System.Obsolete>
    sub Goo()
        dim i = Base.i
        dim j = Base.i
    end sub
end class
")
        End function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)>
        Public Async Function TestObsoleteClassFixAll3() As Task
            Await TestInRegularAndScript1Async(
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    sub Goo()
        dim i = {|FixAllInDocument:|}Base.i
    end sub

    sub Bar()
        dim j = Base.i
    end sub
end class
",
"
<System.Obsolete>
class Base
    public shared i as integer
end class

class Derived
    <System.Obsolete>
    sub Goo()
        dim i = Base.i
    end sub

    <System.Obsolete>
    sub Bar()
        dim j = Base.i
    end sub
end class
")
        End function
    End Class
End Namespace
