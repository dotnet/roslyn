﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.UseAutoProperty
    Public Class UseAutoPropertyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New UseAutoPropertyAnalyzer(), New UseAutoPropertyCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleGetter1() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    readonly property P as integer
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleGetter2() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim i as Integer
    [|readonly property P as integer
        get
            return i
        end get
    end property|]
end class",
"class Class1
    readonly property P as integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleSetter() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        set
            i = value
        end set \end property \end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetter() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
        set
            i = value
        end set
    end property
end class",
"class Class1
    property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestInitializer() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim i as Integer = 1
    [|readonly property P as integer
        get
            return i
        end get
    end property|]
end class",
"class Class1
    readonly property P as integer = 1
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestInitializer_VB9() As Task
            Await TestMissingAsync(
"class Class1
    dim [|i|] as Integer = 1
    readonly property P as integer
        get
            return i
        end get
    end property
end class",
New TestParameters(VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic9)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestReadOnlyField() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as integer|]
    property P as integer
        get
            return i
        end get
    end property
end class",
"class Class1
    ReadOnly property P as integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestReadOnlyField_VB12() As Task
            Await TestMissingAsync(
"class Class1
    [|readonly dim i as integer|]
    property P as integer
        get
            return i
        end get
    end property
end class",
New TestParameters(VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestDifferentValueName() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
        set(v as integer)
            i = v
        end set
    end property
end class",
"class Class1
    property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleGetterWithMe() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return me.i
        end get
    end property
end class",
"class Class1
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSingleSetterWithMe() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        set
            me.i = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetterWithMe() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return me.i
        end get
        set
            me.i = value
 end property
end class",
"class Class1
    property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterWithMutipleStatements() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            Foo()
            return i
        end get
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSetterWithMutipleStatements() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        set
            Foo()
            i = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestSetterWithMutipleStatementsAndGetterWithSingleStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            Return i
        end get

        set
            Foo()
            i = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestGetterAndSetterUseDifferentFields() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    dim j as Integer
    property P as Integer
        get
            return i
        end get
        set
            j = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldAndPropertyHaveDifferentStaticInstance() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|shared i a integer|] 
 property P as Integer
        get
            return i
 end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldUseInRefArgument1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
 end property
    sub M(byref x as integer)
        M(i)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldUseInRefArgument2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
 end property
    sub M(byref x as integer)
        M(me.i) \end sub 
 end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithVirtualProperty() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    public virtual property P as Integer 
 get
    return i
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithConstField() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|const int i|] 
 property P as Integer
        get
            return i
 end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators1() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim [|i|] as integer, j, k
    property P as Integer
        get
            return i
 end property
end class",
"class Class1
    dim j, k
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators2() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim i, [|j|] as integer, k
    property P as Integer
        get
            return j
        end get
    end property
end class",
"class Class1
    dim i as integer, k
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators3() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim i, j, [|k|] as integer
    property P as Integer
        get
            return k
        end get
    end property
end class",
"class Class1
    dim i, j as integer
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldWithMultipleDeclarators4() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    dim i as integer, [|k|] as integer
    property P as Integer
        get
            return k
        end get
    end property
end class",
"class Class1
    dim i as integer
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestFieldAndPropertyInDifferentParts() As Task
            Await TestInRegularAndScriptAsync(
"partial class Class1
    [|dim i as integer|]
end class
partial class Class1
    property P as Integer
        get
            return i
 end property
end class",
"partial class Class1
end class
partial class Class1
    ReadOnly property P as Integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestNotWithFieldWithAttribute() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|<A> dim i as integer|]
    property P as Integer
        get
            return i
 end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestUpdateReferences() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
    end property
    public sub new()
        i = 1
    end sub
end class",
"class Class1
    ReadOnly property P as Integer
    public sub new()
        P = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestUpdateReferencesConflictResolution() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
 public sub new(dim P as integer)
        i = 1
    end sub
end class",
"class Class1
    ReadOnly property P as Integer
    public sub new(dim P as integer)
        Me.P = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInConstructor() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
    end property
    public sub new()
        i = 1
    end sub
end class",
"class Class1
    ReadOnly property P as Integer
    public sub new()
        P = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
 end property
    public sub Foo()
        i = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    public property P as Integer
        get
            return i
 \end property 
 public sub Foo()
        i = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInNotInConstructor3() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    public property P as Integer
        get
            return i
        end get
        set
            i = value
        end set
    end property
    public sub Foo()
        i = 1
    end sub
end class",
"class Class1
    public property P as Integer
    public sub Foo() P = 1 
 end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestAlreadyAutoProperty() As Task
            Await TestMissingInRegularAndScriptAsync("Class Class1
    Public Property [|P|] As Integer
End Class")
        End Function
    End Class
End Namespace
