' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class GraphNodeIdTests
        Private Sub AssertMarkedNodeIdIs(code As String, expectedId As String, Optional language As String = "C#", Optional symbolTransform As Func(Of ISymbol, ISymbol) = Nothing)
            Using testState = New ProgressionTestState(
                <Workspace>
                    <Project Language=<%= language %> CommonReferences="true" FilePath="Z:\Project.csproj">
                        <Document FilePath="Z:\Project.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>)

                Dim graph = testState.GetGraphWithMarkedSymbolNode(symbolTransform)
                Dim node = graph.Nodes.Single()
                Assert.Equal(expectedId, node.Id.ToString())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub SimpleType()
            AssertMarkedNodeIdIs("namespace N { class $$C { } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub NestedType()
            AssertMarkedNodeIdIs("namespace N { class C { class $$E { } } }", "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=E ParentType=C))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithSimpleArrayType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(int[] p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=(Name=Int32 ArrayRank=1 ParentType=Int32))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithNestedArrayType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(int[][,] p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=(Name=Int32 ArrayRank=1 ParentType=(Name=Int32 ArrayRank=2 ParentType=Int32)))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithPointerType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { struct S { } unsafe void $$M(S** p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=S Indirection=2 ParentType=C))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithVoidPointerType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { unsafe void $$M(void* p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=(Name=Void Indirection=1))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithGenericTypeParameters()
            AssertMarkedNodeIdIs(
                "namespace N { class C<T> { void $$M<U>(T t, U u) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=C GenericParameterCount=1) Member=(Name=M GenericParameterCount=1 OverloadingParameters=[(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=C GenericParameterCount=1) ParameterIdentifier=0),(ParameterIdentifier=0)]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(547263)>
        Public Sub MemberWithParameterTypeConstructedWithMemberTypeParameter()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M<T>(T t, System.Func<T, int> u) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M GenericParameterCount=1 OverloadingParameters=[(ParameterIdentifier=0),(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=(Name=Func GenericParameterCount=2 GenericArguments=[(ParameterIdentifier=0),(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32)]))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithArraysOfGenericTypeParameters()
            AssertMarkedNodeIdIs(
                "namespace N { class C<T> { void $$M<U>(T[] t, U[] u) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=C GenericParameterCount=1) Member=(Name=M GenericParameterCount=1 OverloadingParameters=[(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(ArrayRank=1 ParentType=(Type=(Name=C GenericParameterCount=1) ParameterIdentifier=0))),(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(ArrayRank=1 ParentType=(ParameterIdentifier=0)))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithArraysOfGenericTypeParameters2()
            AssertMarkedNodeIdIs(
                "namespace N { class C<T> { void $$M<U>(T[][,] t, U[][,] u) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(Name=C GenericParameterCount=1) Member=(Name=M GenericParameterCount=1 OverloadingParameters=[(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(ArrayRank=1 ParentType=(ArrayRank=2 ParentType=(Type=(Name=C GenericParameterCount=1) ParameterIdentifier=0)))),(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=(ArrayRank=1 ParentType=(ArrayRank=2 ParentType=(ParameterIdentifier=0))))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub MemberWithGenericType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(System.Collections.Generic.List<int> p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System.Collections.Generic Type=(Name=List GenericParameterCount=1 GenericArguments=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System Type=Int32)]))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(616549)>
        Public Sub MemberWithDynamicType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(dynamic d) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Namespace=System Type=Object)]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(616549)>
        Public Sub MemberWithGenericTypeOfDynamicType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(System.Collections.Generic.List<dynamic> p) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Assembly=file:///Z:/FxReferenceAssembliesUri Namespace=System.Collections.Generic Type=(Name=List GenericParameterCount=1 GenericArguments=[(Namespace=System Type=Object)]))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(616549)>
        Public Sub MemberWithArrayOfDynamicType()
            AssertMarkedNodeIdIs(
                "namespace N { class C { void $$M(dynamic[] d) { } } }",
                "(Assembly=file:///Z:/CSharpAssembly1.dll Namespace=N Type=C Member=(Name=M OverloadingParameters=[(Namespace=System Type=(Name=Object ArrayRank=1 ParentType=Object))]))")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(547234)>
        Public Sub ErrorType()
            AssertMarkedNodeIdIs(
                "Class $$C : Inherits D : End Class",
                "Type=D",
                LanguageNames.VisualBasic,
                Function(s) DirectCast(s, INamedTypeSymbol).BaseType)
        End Sub
    End Class
End Namespace
