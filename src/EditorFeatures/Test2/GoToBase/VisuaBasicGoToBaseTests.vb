' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToBase
    <[UseExportProvider]>
    Public Class VisualBasicGoToBaseTests
        Inherits GoToBaseTestsBase
        Private Overloads Async Function TestAsync(source As String, Optional shouldSucceed As Boolean = True,
                                                   Optional metadataDefinitions As String() = Nothing) As Task
            Await TestAsync(source, LanguageNames.VisualBasic, shouldSucceed, metadataDefinitions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestEmptyFile() As Task
            Await TestAsync("$$", shouldSucceed:=False)
        End Function

#Region "Classes And Interfaces"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClass() As Task
            Await TestAsync(
"class $$C
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClass() As Task
            Await TestAsync(
"mustinherit class [|C|]
end class

class $$D 
    inherits C
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractClassFromInterface() As Task
            Await TestAsync(
"interface [|I|]
end interface
mustinherit class [|C|] 
    implements I
end class
class $$D 
    inherits C
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSealedClass() As Task
            Await TestAsync(
"class [|D|]
end class
NotInheritable class $$C 
    inherits D
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithEnum() As Task
            Await TestAsync(
"enum $$C
end enum")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithNonAbstractClass() As Task
            Await TestAsync(
"class [|C|]
end class

class $$D 
    inherits C
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleClassImplementation() As Task
            Await TestAsync(
"class $$C 
    implements I
end class
interface [|I|]
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoClassImplementations() As Task
            Await TestAsync(
"class $$C 
    implements I
end class
class D 
    implements I
end class
interface [|I|]
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestClassHierarchyWithParentSiblings() As Task
            Await TestAsync(
"class E 
    inherits D
end class
class $$D
    inherits B
end class
class [|B|]
    inherits A
end class
class C
    inherits A
end class
class [|A|]
    implements I2
end class
interface [|I2|]
    inherits I
end interface
interface I1
    inherits I
end interface
interface [|I|]
    inherits J1, J2
end interface
interface [|J1|]
end interface
interface [|J2|]
end interface")
        End Function

#End Region

#Region "Structures"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithStruct() As Task
            Await TestAsync(
"structure $$S
end structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSingleStructImplementation() As Task
            Await TestAsync(
"structure $$S
    implements I
end structure
interface [|I|]
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestStructWithInterfaceHierarchy() As Task
            Await TestAsync(
"structure $$S 
    implements I2
end interface
interface [|I2|] 
    inherits I
end interface
interface I1
    inherits I
end interface
interface [|I|]
    inherits J1, J2
end interface
interface [|J1|]
end interface
interface [|J2|]
end interface")
        End Function

#End Region

#Region "Methods"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementation_01() As Task
            Await TestAsync(
"class C
    implements I
    public sub $$M() implements I.M
    end sub
end class
interface I
    sub [|M|]()
end interface")
        End Function
        Public Async Function TestWithOneMethodImplementation_02() As Task
            Await TestAsync(
"class C
    implements I
    public sub M()
    end sub
    private sub $$I_M() implements I.M
    end sub
end class
interface I
    sub [|M|]()
end interface")
        End Function
        Public Async Function TestWithOneMethodImplementation_03() As Task
            Await TestAsync(
"class C
    implements I
    public sub $$M()
    end sub
    private sub I_M() implements I.M
    end sub
end class
interface I
    sub M()
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestInterfaceWithOneMethodOverload() As Task
            Await TestAsync(
"interface J
    inherits I 
    overloads sub $$M()
end interface
interface I
    sub M()
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneMethodImplementationInStruct() As Task
            Await TestAsync(
"structure S
    implements I
        sub $$M() implements I.M
        end sub
end structure
interface I
    sub [|M|]()
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithTwoMethodImplementations() As Task
            Await TestAsync(
"class C
    implements I
        sub $$M() implements I.M
        end sub
end class
class D 
    implements I
        sub M() implements I.M
        end sub
end class
interface I
    sub [|M|]()
end interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_01() As Task
            Await TestAsync(
"class C
    inherits D
    public overrides sub $$M()
    end sub
end class
class D 
    public overridable sub [|M|]()
    end sub
    public overridable sub M(a as integer)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideWithOverloads_02() As Task
            Await TestAsync(
"class C
    inherits D
    public overrides sub $$M(a as integer)
    end sub
end class
class D 
    public overridable sub M()
    end sub
    public overridable sub [|M|](a as integer)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_01() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Sub $$M() Implements I.M
    End Sub
    Public Sub M(a As Integer) Implements I.M
    End Sub
End Class
Interface I
    Sub [|M|]()
    Sub M(a As Integer)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestImplementWithOverloads_02() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Sub M() Implements I.M
    End Sub
    Public Sub $$M(a As Integer) Implements I.M
    End Sub
End Class
Interface I
    Sub M()
    Sub [|M|](a As Integer)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnBaseClass() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub [|M|]() Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Public Overrides Sub $$M()
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnBaseClass() As Task
            ' We should not find hidden methods 
            ' and methods in interfaces if hidden below but the nested class does not implement the interface.
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub N() Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Public Shadows Sub $$N()
    End Sub
End Class
Interface I
    Sub M()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationWithInterfaceOnDerivedClass() As Task
            Await TestAsync(
"Class C
    Public Overridable Sub [|M|]()
    End Sub
End Class
Class D
    Inherits C
    Implements I
    Public Overrides Sub $$M Implements I.M
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenWithInterfaceOnDerivedClass() As Task
            ' We should not find a hidden method.
            Await TestAsync(
"Class C
    Public Overridable Sub M|)
    End Sub
End Class
Class D
    Inherits C
    Implements I
    Public Shadows Sub $$M Implements I.M
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodImplementationAndInterfaceImplementedOnDerivedType() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub [|M|]() Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Implements I
    Public Overrides Sub $$M()
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenAndInterfaceImplementedOnDerivedType() As Task
            ' We should not find hidden methods.
            ' We should not find methods of interfaces no implemented by the method symbol.
            ' In this example, 
            ' Dim i As I = New D()
            ' i.M()
            ' calls the method from C not from D.
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub M() Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Implements I
    Public Shadows Sub $$M()
    End Sub
End Class
Interface I
    Sub M()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithVirtualMethodHiddenAndInterfaceAndMethodImplementedOnDerivedType() As Task
            ' We should not find hidden methods but should find the interface method.
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub M() Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Implements I
    Public Shadows Sub $$M() Implements I.M
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithAbstractMethodImplementation() As Task
            Await TestAsync(
"MustInherit Class C
    Implements I
    Public MustOverride Sub [|N|]() Implements I.M
End Class
Class D
    Inherits C
    Public Overrides Sub $$N()
    End Sub
End Class
Interface I
    Sub [|M|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithSimpleMethod() As Task
            Await TestAsync(
"class C 
    public sub $$M()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnBase() As Task
            Await TestAsync(
"class C 
    public overridable sub [|M|]()
    end sub
end class

class D
    inherits C
    public overrides sub $$M()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverridableMethodOnImplementation() As Task
            Await TestAsync(
"class C 
    public overridable sub $$M()
    end sub
end class

class D
    inherits C
    public overrides sub M()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithIntermediateAbstractOverrides() As Task
            Await TestAsync(
"MustInherit Class A
    Public Overridable Sub [|M|]()
    End Sub
End Class
MustInherit Class B
    Inherits A
    Public MustOverride Overrides Sub M()
End Class
NotInheritable Class C1
    Inherits B
    Public Overrides Sub M()
    End Sub
End Class
NotInheritable Class C2
    Inherits A
    Public Overrides Sub $$M()
        MyBase.M()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverloadsOverrdiesAndInterfaceImplementation_01() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub [|N|]() Implements I.M
    End Sub
    Public Overridable Sub N(i As Integer) Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Public Overrides Sub $$N()
    End Sub
    Public Overrides Sub N(i As Integer)
    End Sub
End Class
Interface I
    Sub [|M|]()
    Sub M(i As Integer)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOverloadsOverrdiesAndInterfaceImplementation_02() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Overridable Sub N() Implements I.M
    End Sub
    Public Overridable Sub [|N|](i As Integer) Implements I.M
    End Sub
End Class
Class D
    Inherits C
    Public Overrides Sub N()
    End Sub
    Public Overrides Sub $$N(i As Integer)
    End Sub
End Class
Interface I
    Sub M()
    Sub [|M|](i As Integer)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestOverrideOfMethodFromMetadata() As Task
            Await TestAsync(
"Imports System
Class C 
    Public Overrides Function $$ToString() As String
        Return base.ToString();
    End Function
End Class
", metadataDefinitions:={"mscorlib:Object.ToString"})
        End Function

#End Region

#Region "Properties and Events"
        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementation() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Event $$E() Implements I.E
End Class
Interface I
    Event [|E|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOneEventImplementationInStruct() As Task
            Await TestAsync(
"Structure C
    Implements I
    Public Event $$E() Implements I.E
End Structure
Interface I
    Event [|E|]()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementation() As Task
            Await TestAsync(
"Class C
    Implements I
    Public Property $$P As Integer Implements I.P
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
Interface I
    Property [|P|]() As Integer
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.GoToBase)>
        Public Async Function TestWithOnePropertyImplementationInStruct() As Task
            Await TestAsync(
"Structure C
    Implements I
    Public Property $$P As Integer Implements I.P
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Structure
Interface I
    Property [|P|]() As Integer
End Interface")
        End Function
#End Region

    End Class
End Namespace
