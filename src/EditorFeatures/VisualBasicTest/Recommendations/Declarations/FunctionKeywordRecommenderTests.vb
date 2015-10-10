' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class FunctionKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInMethodDeclaration()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInNamespace()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInEnum()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInStructure()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterPublic()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterProtected()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterPrivate()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterProtectedFriend()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverloads()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverrides()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverridable()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterNotOverridable()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterMustOverride()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterMustOverrideOverrides()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterNotOverridableOverrides()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterConst()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterDefault()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterMustInherit()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterNotInheritable()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterNarrowing()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterWidening()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterReadOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterWriteOnly()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterCustom()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterShared()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(543270)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInDelegateCreation()
            Dim code =
<ModuleDeclaration>
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2( |
    End Sub
 
    Delegate Sub Foo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</ModuleDeclaration>


            VerifyRecommendationsContain(code, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverridesModifier()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlock()
            Dim code =
<ClassDeclaration>
Function M() As Boolean
    Try
    Finally
        Exit |
</ClassDeclaration>

            VerifyRecommendationsMissing(code, "Function")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Function M() As Boolean
        Exit
 |
</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>
Function M() As Boolean
        Exit _
 |
</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(547254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAsync()
            VerifyRecommendationsContain(<ClassDeclaration>Async |</ClassDeclaration>, "Function")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterIterator()
            VerifyRecommendationsContain(<ClassDeclaration>Iterator |</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(531638)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InModuleAfterMethod()
            VerifyRecommendationsContain(
<File>
Module Program
    Sub foo()

    End Sub
    |
End Module
</File>, "Function")
        End Sub

        <WorkItem(674791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Function")
        End Sub

    End Class
End Namespace
