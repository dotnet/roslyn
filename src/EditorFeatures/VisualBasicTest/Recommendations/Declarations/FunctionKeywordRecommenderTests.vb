' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class FunctionKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInMethodDeclarationTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInNamespaceTest()
            VerifyRecommendationsMissing(<NamespaceDeclaration>|</NamespaceDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotInEnumTest()
            VerifyRecommendationsMissing(<EnumDeclaration>|</EnumDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInStructureTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>|</ModuleDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterPublicTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterProtectedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterPrivateTest()
            VerifyRecommendationsContain(<ClassDeclaration>Private |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterProtectedFriendTest()
            VerifyRecommendationsContain(<ClassDeclaration>Protected Friend |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverloadsTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overloads |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overridable |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterNotOverridableTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterMustOverrideTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterMustOverrideOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>MustOverride Overrides |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterNotOverridableOverridesTest()
            VerifyRecommendationsContain(<ClassDeclaration>NotOverridable Overrides |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterConstTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterDefaultTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Default |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterMustInheritTest()
            VerifyRecommendationsMissing(<ClassDeclaration>MustInherit |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterNotInheritableTest()
            VerifyRecommendationsMissing(<ClassDeclaration>NotInheritable |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterNarrowingTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Narrowing |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterWideningTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Widening |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterReadOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReadOnly |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterWriteOnlyTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WriteOnly |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionNotAfterCustomTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Custom |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterSharedTest()
            VerifyRecommendationsContain(<ClassDeclaration>Shared |</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionInDelegateCreationTest()
            Dim code =
<ModuleDeclaration>
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2( |
    End Sub
 
    Delegate Sub Goo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</ModuleDeclaration>

            VerifyRecommendationsContain(code, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionAfterOverridesModifierTest()
            VerifyRecommendationsContain(<ClassDeclaration>Overrides Public |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<ClassDeclaration>
Function M() As Boolean
    Try
    Finally
        Exit |
</ClassDeclaration>

            VerifyRecommendationsMissing(code, "Function")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>
Function M() As Boolean
        Exit
 |
</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>
Function M() As Boolean
        Exit _
 |
</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>
Function M() As Boolean
        Exit _ ' Test
 |
</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterAsyncTest()
            VerifyRecommendationsContain(<ClassDeclaration>Async |</ClassDeclaration>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterIteratorTest()
            VerifyRecommendationsContain(<ClassDeclaration>Iterator |</ClassDeclaration>, "Function")
        End Sub

        <WorkItem(531638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531638")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InModuleAfterMethodTest()
            VerifyRecommendationsContain(
<File>
Module Program
    Sub goo()

    End Sub
    |
End Module
</File>, "Function")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Function")
        End Sub

        <WorkItem(20837, "https://github.com/dotnet/roslyn/issues/20837")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExtensionAttribute()
            VerifyRecommendationsContain(<ClassDeclaration>&lt;Extension&gt; |</ClassDeclaration>, "Function")
        End Sub
    End Class
End Namespace
