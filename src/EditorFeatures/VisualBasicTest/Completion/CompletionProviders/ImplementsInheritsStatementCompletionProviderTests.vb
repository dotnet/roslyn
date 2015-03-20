' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ImplementsInheritsStatementCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function CreateCompletionProvider() As ICompletionProvider
            Return New ImplementsInheritsStatementCompletionProvider()
        End Function

        Private Const s_unicodeEllipsis = ChrW(&H2026)

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterInherits()
            Dim text = <text>Public Class Base
End Class

Class Derived
    Inherits $$
End Class</text>.Value

            VerifyItemExists(text, "Base")
            VerifyItemIsAbsent(text, "Derived")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterInheritsDotIntoClass()
            Dim text = <text>Public Class Base
    Public Class Nest
    End Class
End Class

Class Derived
    Inherits Base.$$
End Class</text>.Value

            VerifyItemExists(text, "Nest")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterImplements()
            Dim text = <text>Public Interface IFoo
End Interface

Class C
    Implements $$
End Class</text>.Value

            VerifyItemExists(text, "IFoo")
        End Sub

        <WorkItem(995986)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasedInterfaceAfterImplements()
            Dim text = <text>Imports IAlias = IFoo
Public Interface IFoo
End Interface

Class C
    Implements $$
End Class</text>.Value

            VerifyItemExists(text, "IAlias")
        End Sub

        <WorkItem(995986)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasedNamespaceAfterImplements()
            Dim text = <text>Imports AliasedNS = NS1
Namespace NS1
    Public Interface IFoo
    End Interface

    Class C
        Implements $$
    End Class
End Namespace</text>.Value

            VerifyItemExists(text, "AliasedNS")
        End Sub

        <WorkItem(995986)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasedClassAfterInherits()
            Dim text = <text>Imports AliasedClass = Base
Public Class Base
End Interface

Class C
    Inherits $$
End Class</text>.Value

            VerifyItemExists(text, "AliasedClass")
        End Sub

        <WorkItem(995986)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasedNamespaceAfterInherits()
            Dim text = <text>Imports AliasedNS = NS1
Namespace NS1
Public Class Base
End Interface

Class C
    Inherits $$
    End Class
End Namespace</text>.Value

            VerifyItemExists(text, "AliasedNS")
        End Sub

        <WorkItem(995986)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AliasedClassAfterInherits2()
            Dim text = <text>Imports AliasedClass = NS1.Base
Namespace NS1
Public Class Base
End Interface

Class C
    Inherits $$
    End Class
End Namespace</text>.Value
            VerifyItemExists(text, "AliasedClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterImplementsComma()
            Dim text = <text>Public Interface IFoo
End Interface

Public Interface IBar
End interface

Class C
    Implements IFoo, $$
End Class</text>.Value

            VerifyItemExists(text, "IBar")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ClassContainingInterface()
            Dim text = <text>Public Class Base
    Public Interface Nest
    End Class
End Class

Class Derived
    Implements $$
End Class</text>.Value

            VerifyItemExists(text, "Base")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoClassNotContainingInterface()
            Dim text = <text>Public Class Base
End Class

Class Derived
    Implements $$
End Class</text>.Value

            VerifyItemIsAbsent(text, "Base")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub GenericClass()
            Dim text = <text>Public Class base(Of T)

End Class

Public Class derived
    Inherits $$
End Class</text>.Value
            VerifyItemExists(text, "base(Of " + s_unicodeEllipsis + ")")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub GenericInterface()
            Dim text = <text>Public Interface IFoo(Of T)

End Interface

Public Class bar
    Implements $$
End Class</text>.Value
            VerifyItemExists(text, "IFoo(Of " + s_unicodeEllipsis + ")")
        End Sub

        <WorkItem(546610)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IncompleteClassDeclaration()
            Dim text = <text>Public Interface IFoo
End Interface
Public Interface IBar
End interface
Class C
    Implements IFoo,$$</text>.Value
            VerifyItemExists(text, "IBar")
        End Sub

        <WorkItem(546611)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotNotInheritable()
            Dim text = <text>Public NotInheritable Class D
End Class
Class C
    Inherits $$</text>.Value
            VerifyItemIsAbsent(text, "D")
        End Sub

        <WorkItem(546802)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordIdentifiersShownUnescaped()
            Dim text = <text>Public Class [Inherits]
End Class
Class C
    Inherits $$</text>.Value
            VerifyItemExists(text, "Inherits")
        End Sub

        <WorkItem(546802)>
<Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordIdentifiersCommitEscaped()
            Dim text = <text>Public Class [Inherits]
End Class
Class C
    Inherits $$</text>.Value

            Dim expected = <text>Public Class [Inherits]
End Class
Class C
    Inherits [Inherits]</text>.Value

            VerifyProviderCommit(text, "Inherits", expected, "."c, "")
        End Sub

        <WorkItem(546801)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Modules()
            Dim text = <text>Module Module1
Sub Main()
End Sub
End Module
Module Module2
  Class Bx
  End Class

End Module 

Class Max
  Class Bx
  End Class
End Class

Class A
Inherits $$

End Class
</text>.Value
            VerifyItemExists(text, "Module2")
            VerifyItemIsAbsent(text, "Module1")
        End Sub

        <WorkItem(530726)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotShowNamespaceWithNoApplicableClasses()
            Dim text = <text>Namespace N
    Module M
    End Module
End Namespace
Class C
    Inherits $$
End Class

</text>.Value
            VerifyItemIsAbsent(text, "N")
        End Sub

        <WorkItem(530725)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CheckStructContents()
            Dim text = <text>Namespace N
    Public Structure S1
        Public Class B
        End Class
    End Structure
    Public Structure S2
    End Structure
End Namespace
Class C
    Inherits N.$$
End Class


</text>.Value
            VerifyItemIsAbsent(text, "S2")
            VerifyItemExists(text, "S1")
        End Sub

        <WorkItem(530724)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NamespaceContainingInterface()
            Dim text = <text>Namespace N
    Interface IFoo
    End Interface
End Namespace
Class C
    Implements $$
End Class



</text>.Value
            VerifyItemExists(text, "N")
        End Sub

        <WorkItem(531256)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyInterfacesForInterfaceInherits1()
            Dim text =
<code>
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IFoo
    Inherits $$
</code>.Value
            VerifyItemExists(text, "ITestInterface")
            VerifyItemIsAbsent(text, "TestClass")
        End Sub

        <WorkItem(1036374)>
        <Fact()>
        Public Sub InterfaceCircularInheritance()
            Dim text =
<code>
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface A(Of T)
    Inherits A(Of A(Of T))
    Interface B
        Inherits $$
    End Interface
End Interface
</code>.Value
            VerifyItemExists(text, "ITestInterface")
            VerifyItemIsAbsent(text, "TestClass")
        End Sub

        <WorkItem(531256)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyInterfacesForInterfaceInherits2()
            Dim text =
<code>
Interface ITestInterface
End Interface

Class TestClass
End Class

Interface IFoo
    Implements $$
</code>.Value
            VerifyItemIsAbsent(text, "ITestInterface")
            VerifyItemIsAbsent(text, "TestClass")
        End Sub

        <WorkItem(547291)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitGenericOnParen()
            Dim text =
<code>
Class G(Of T)
End Class

Class DG
    Inherits $$
End Class

</code>.Value

            Dim expected =
<code>
Class G(Of T)
End Class

Class DG
    Inherits G(
End Class

</code>.Value

            VerifyProviderCommit(text, "G(Of …)", expected, "("c, "")
        End Sub

        <WorkItem(579186)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterImplementsWithCircularInheritance()
            Dim text = <text>Interface I(Of T)
End Interface
 
Class C(Of T)
    Class D
        Inherits C(Of D)
        Implements $$
    End Class
End Class</text>.Value

            VerifyItemExists(text, "I(Of …)")
        End Sub

        <WorkItem(622563)>
<Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitNonGenericOnParen()
            Dim text =
<code>
Class G
End Class

Class DG
    Inherits $$
End Class

</code>.Value

            Dim expected =
<code>
Class G
End Class

Class DG
    Inherits G
End Class

</code>.Value

            VerifyProviderCommit(text, "G", expected, "("c, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterInheritsWithCircularInheritance()
            Dim text = <text>Class B
End Class
 
Class C(Of T)
    Class D
        Inherits C(Of D)
        Inherits $$
    End Class
End Class</text>.Value

            VerifyItemExists(text, "B")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ClassesInsideSealedClasses()
            Dim text = <text>Public NotInheritable Class G
    Public Class H

    End Class
End Class

Class SomeClass
    Inherits $$

End Class </text>.Value

            VerifyItemExists(text, "G")
        End Sub

        <WorkItem(638762)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ClassWithinNestedStructs()
            Dim text = <text>Structure somestruct
    Structure Inner
        Class FinallyAClass
        End Class
    End Structure
 
End Structure
Class SomeClass
    Inherits $$
 
End</text>.Value

            VerifyItemExists(text, "somestruct")
        End Sub
    End Class
End Namespace


