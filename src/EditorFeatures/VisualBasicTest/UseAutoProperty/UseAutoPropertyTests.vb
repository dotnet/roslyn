' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseAutoProperty
    Public Class UseAutoPropertyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseAutoPropertyAnalyzer(), New VisualBasicUseAutoPropertyCodeFixProvider())
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
        <WorkItem(26256, "https://github.com/dotnet/roslyn/issues/26256")>
        Public Async Function TestSingleGetter3() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    shared dim i as Integer
    [|shared property P as integer
        get
            return i
        end get
    end property|]
end class",
"class Class1
    shared ReadOnly property P as integer
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestNullable1() As Task
            ' ⚠ The expected outcome of this test should not change.
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as MutableInt?|]
    readonly property P as MutableInt?
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestNullable2() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as MutableInt?|]
    readonly property P as MutableInt?
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Value As Integer
End Structure",
"class Class1
    readonly property P as MutableInt?
end class
Structure MutableInt
    Public Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestNullable3() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as integer?|]
    readonly property P as integer?
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as integer?
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestNullable4() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as integer?|]
    readonly property P as integer?
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as integer?
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestNullable5() As Task
            ' Recursive type check
            Await TestMissingInRegularAndScriptAsync(
"Imports System
class Class1
    [|dim i as Nullable(Of Nullable(Of MutableInt))|]
    readonly property P as Nullable(Of Nullable(Of MutableInt))
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestMutableValueType1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as MutableInt|]
    readonly property P as MutableInt
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestMutableValueType2() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as MutableInt|]
    readonly property P as MutableInt
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Value As Integer
End Structure",
"class Class1
    readonly property P as MutableInt
end class
Structure MutableInt
    Public Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestMutableValueType3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as MutableInt|]
    readonly property P as MutableInt
        get
            return i
        end get
    end property
end class
Structure MutableInt
    Public Property Value As Integer
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestErrorType1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as ErrorType|]
    readonly property P as ErrorType
        get
            return i
        end get
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestErrorType2() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as ErrorType|]
    readonly property P as ErrorType
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as ErrorType
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestErrorType3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as ErrorType?|]
    readonly property P as ErrorType?
        get
            return i
        end get
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestErrorType4() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|readonly dim i as ErrorType?|]
    readonly property P as ErrorType?
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as ErrorType?
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28511, "https://github.com/dotnet/roslyn/issues/28511")>
        Public Async Function TestErrorType5() As Task
            Await TestInRegularAndScriptAsync(
"class Class1
    [|dim i as ErrorType()|]
    readonly property P as ErrorType()
        get
            return i
        end get
    end property
end class",
"class Class1
    readonly property P as ErrorType()
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
        <WorkItem(26256, "https://github.com/dotnet/roslyn/issues/26256")>
        Public Async Function TestInitializer_AsNew() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
class Class1
    dim i as new EventArgs()
    [|readonly property P as EventArgs
        get
            return i
        end get
    end property|]
end class",
"Imports System
class Class1
    readonly property P as new EventArgs()
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(26256, "https://github.com/dotnet/roslyn/issues/26256")>
        Public Async Function TestInitializer_AsNewDifferentType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
class Class1
    dim i as new EventArgs()
    [|readonly property P as Object
        get
            return i
        end get
    end property|]
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28989, "https://github.com/dotnet/roslyn/issues/28989")>
        Public Async Function TestInitializer_Boolean() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Private _b As Boolean = True
    Public Property [|P|]() As Boolean
        Get
            Return _b
        End Get
        Set(value As Boolean)
            _b = value
        End Set
    End Property
End Class",
"Public Class C
    Public Property P() As Boolean = True
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28989, "https://github.com/dotnet/roslyn/issues/28989")>
        Public Async Function TestInitializer_BooleanWithComments() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Private _b As Boolean = True 'Comments1
    Public Property [|P|]() As Boolean 'Comments2
        Get
            Return _b
        End Get
        Set(value As Boolean)
            _b = value
        End Set
    End Property
End Class",
"Public Class C
    Public Property P() As Boolean = True 'Comments2 'Comments1
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(28989, "https://github.com/dotnet/roslyn/issues/28989")>
        Public Async Function TestInitializer_Multiline() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Dim [|_b|] = {
        ""one"",
        ""two"",
        ""three""}
    Public Property P()
        Get
            Return _b
        End Get
        Set
            _b = Value
        End Set
    End Property
End Class",
"Public Class C
    Public Property P() = {
        ""one"",
        ""two"",
        ""three""}
End Class")
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
            Goo()
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
            Goo()
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
            Goo()
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
    end property
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
    public sub Goo()
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
 public sub Goo()
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
    public sub Goo()
        i = 1
    end sub
end class",
"class Class1
    public property P as Integer
    public sub Goo()
        P = 1
    end sub
end class")
        End Function

        <WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInMultiLineSubLambdaInConstructor() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|dim i as integer|]
    readonly property P as integer
        get
            return i
        end get
    end property

    sub new()
        dim x = sub()
                    i = 1
                end sub
    end sub
end class",
"class C
    property P as integer

    sub new()
        dim x = sub()
                    P = 1
                end sub
    end sub
end class")
        End Function

        <WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInMultiLineFunctionLambdaInConstructor() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|dim i as integer|]
    readonly property P as integer
        get
            return i
        end get
    end property

    sub new()
        dim x = function()
                    i = 1
                    return 0
                end function
    end sub
end class",
"class C
    property P as integer

    sub new()
        dim x = function()
                    P = 1
                    return 0
                end function
    end sub
end class")
        End Function

        <WorkItem(30108, "https://github.com/dotnet/roslyn/issues/30108")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestWriteInSingleLineSubLambdaInConstructor() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|dim i as integer|]
    readonly property P as integer
        get
            return i
        end get
    end property

    sub new()
        dim x = sub() i = 1
    end sub
end class",
"class C
    property P as integer

    sub new()
        dim x = sub() P = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestReadInSingleLineFunctionLambdaInConstructor() As Task
            ' Since the lambda is a function lambda, the `=` is a comparison, not an assignment.
            Await TestInRegularAndScriptAsync(
"class C
    [|dim i as integer|]
    readonly property P as integer
        get
            return i
        end get
    end property

    sub new()
        dim x = function() i = 1
    end sub
end class",
"class C
    readonly property P as integer

    sub new()
        dim x = function() P = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestAlreadyAutoProperty() As Task
            Await TestMissingInRegularAndScriptAsync("Class Class1
    Public Property [|P|] As Integer
End Class")
        End Function

        <WorkItem(23735, "https://github.com/dotnet/roslyn/issues/23735")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function ExplicitInterfaceImplementation() As Task
            Await TestInRegularAndScriptAsync("
Namespace RoslynSandbox
    Public Interface IFoo
        ReadOnly Property Bar() As Object
    End Interface

    Friend Class Foo
        Implements IFoo

        Private [|_bar|] As Object

        Private ReadOnly Property Bar() As Object Implements IFoo.Bar
            Get
                Return _bar
            End Get
        End Property

        Public Sub New(bar As Object)
            _bar = bar
        End Sub
    End Class
End Namespace
",
"
Namespace RoslynSandbox
    Public Interface IFoo
        ReadOnly Property Bar() As Object
    End Interface

    Friend Class Foo
        Implements IFoo

        Private ReadOnly Property Bar() As Object Implements IFoo.Bar

        Public Sub New(bar As Object)
            Me.Bar = bar
        End Sub
    End Class
End Namespace
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(25401, "https://github.com/dotnet/roslyn/issues/25401")>
        Public Async Function TestGetterAccessibilityDiffers() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        protected get
            return i
        end get
        set
            i = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        <WorkItem(25401, "https://github.com/dotnet/roslyn/issues/25401")>
        Public Async Function TestSetterAccessibilityDiffers() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Class1
    [|dim i as integer|]
    property P as Integer
        get
            return i
        end get
        protected set
            i = value
        end set
    end property
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
        Public Async Function TestLeadingBlankLinesRemoved() As Task
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
    End Class
End Namespace
