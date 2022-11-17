' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.ImmutableArrayExtensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingGenericTypeParameters : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assembly = MetadataTestHelpers.LoadFromBytes(TestMetadata.ResourcesNet40.mscorlib)
            Dim module0 = assembly.Modules(0)

            Dim objectType = module0.GlobalNamespace.GetMembers("System").
                OfType(Of NamespaceSymbol).Single().
                GetTypeMembers("Object").Single()

            Assert.Equal(0, objectType.Arity)
            Assert.Equal(0, objectType.TypeParameters.Length)
            Assert.Equal(0, objectType.TypeArguments.Length)

            assembly = MetadataTestHelpers.LoadFromBytes(TestResources.General.MDTestLib1)
            module0 = assembly.Modules(0)

            Dim C1 = module0.GlobalNamespace.GetTypeMembers("C1").Single()

            Assert.Equal(1, C1.Arity)
            Assert.Equal(1, C1.TypeParameters.Length)
            Assert.Equal(1, C1.TypeArguments.Length)

            Dim C1_T = C1.TypeParameters(0)

            Assert.Equal(C1_T, C1.TypeArguments(0))

            Assert.Null(C1_T.BaseType)
            Assert.Equal(assembly, C1_T.ContainingAssembly)
            Assert.Equal(module0.GlobalNamespace, C1_T.ContainingNamespace) 'Null(C1_T.ContainingNamespace)
            Assert.Equal(C1, C1_T.ContainingSymbol)
            Assert.Equal(C1, C1_T.ContainingType)
            Assert.Equal(Accessibility.NotApplicable, C1_T.DeclaredAccessibility)
            Assert.Equal("C1_T", C1_T.Name)
            Assert.Equal("C1_T", C1_T.ToTestDisplayString())
            Assert.Equal(0, C1_T.GetMembers().Length())
            Assert.Equal(0, C1_T.GetMembers("goo").Length())
            Assert.Equal(0, C1_T.GetTypeMembers().Length())
            Assert.Equal(0, C1_T.GetTypeMembers("goo").Length())
            Assert.Equal(0, C1_T.GetTypeMembers("goo", 1).Length())
            Assert.False(C1_T.HasConstructorConstraint)
            Assert.False(C1_T.HasReferenceTypeConstraint)
            Assert.False(C1_T.HasValueTypeConstraint)
            Assert.Equal(0, C1_T.Interfaces.Length)
            Assert.True(C1_T.IsDefinition)
            Assert.False(C1_T.IsMustOverride)
            Assert.False(C1_T.IsNamespace)
            Assert.False(C1_T.IsNotOverridable)
            Assert.False(C1_T.IsOverridable)
            Assert.False(C1_T.IsOverrides)
            Assert.False(C1_T.IsShared)
            Assert.True(C1_T.IsType)
            Assert.Equal(SymbolKind.TypeParameter, C1_T.Kind)
            Assert.Equal(0, C1_T.Ordinal)
            Assert.Equal(C1_T, C1_T.OriginalDefinition)
            Assert.Equal(TypeKind.TypeParameter, C1_T.TypeKind)
            Assert.Equal(VarianceKind.None, C1_T.Variance)
            Assert.Same(module0, C1_T.Locations.Single().MetadataModule)
            Assert.Equal(0, C1_T.ConstraintTypes.Length)

            Dim C2 = C1.GetTypeMembers("C2").Single()
            Assert.Equal(1, C2.Arity)
            Assert.Equal(1, C2.TypeParameters.Length)
            Assert.Equal(1, C2.TypeArguments.Length)

            Dim C2_T = C2.TypeParameters(0)

            Assert.Equal("C2_T", C2_T.Name)
            Assert.Equal(C2, C2_T.ContainingType)

            Dim C3 = C1.GetTypeMembers("C3").Single()
            Assert.Equal(0, C3.Arity)
            Assert.Equal(0, C3.TypeParameters.Length)
            Assert.Equal(0, C3.TypeArguments.Length)

            Dim C4 = C3.GetTypeMembers("C4").Single()
            Assert.Equal(1, C4.Arity)
            Assert.Equal(1, C4.TypeParameters.Length)
            Assert.Equal(1, C4.TypeArguments.Length)

            Dim C4_T = C4.TypeParameters(0)

            Assert.Equal("C4_T", C4_T.Name)
            Assert.Equal(C4, C4_T.ContainingType)

            Dim TC2 = module0.GlobalNamespace.GetTypeMembers("TC2").Single()

            Assert.Equal(2, TC2.Arity)
            Assert.Equal(2, TC2.TypeParameters.Length)
            Assert.Equal(2, TC2.TypeArguments.Length)

            Dim TC2_T1 = TC2.TypeParameters(0)
            Dim TC2_T2 = TC2.TypeParameters(1)

            Assert.Equal(TC2_T1, TC2.TypeArguments(0))
            Assert.Equal(TC2_T2, TC2.TypeArguments(1))

            Assert.Equal("TC2_T1", TC2_T1.Name)
            Assert.Equal(TC2, TC2_T1.ContainingType)
            Assert.Equal(0, TC2_T1.Ordinal)

            Assert.Equal("TC2_T2", TC2_T2.Name)
            Assert.Equal(TC2, TC2_T2.ContainingType)
            Assert.Equal(1, TC2_T2.Ordinal)

            Dim C100 = module0.GlobalNamespace.GetTypeMembers("C100").Single()
            Dim T = C100.TypeParameters(0)
            Assert.False(T.HasConstructorConstraint)
            Assert.False(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.Out, T.Variance)

            Dim C101 = module0.GlobalNamespace.GetTypeMembers("C101").Single()
            T = C101.TypeParameters(0)
            Assert.False(T.HasConstructorConstraint)
            Assert.False(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.In, T.Variance)

            Dim C102 = module0.GlobalNamespace.GetTypeMembers("C102").Single()
            T = C102.TypeParameters(0)
            Assert.True(T.HasConstructorConstraint)
            Assert.False(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.None, T.Variance)
            Assert.Equal(0, T.ConstraintTypes.Length)

            Dim C103 = module0.GlobalNamespace.GetTypeMembers("C103").Single()
            T = C103.TypeParameters(0)
            Assert.False(T.HasConstructorConstraint)
            Assert.True(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.None, T.Variance)
            Assert.Equal(0, T.ConstraintTypes.Length)

            Dim C104 = module0.GlobalNamespace.GetTypeMembers("C104").Single()
            T = C104.TypeParameters(0)
            Assert.False(T.HasConstructorConstraint)
            Assert.False(T.HasReferenceTypeConstraint)
            Assert.True(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.None, T.Variance)
            Assert.Equal(0, T.ConstraintTypes.Length)

            Dim C105 = module0.GlobalNamespace.GetTypeMembers("C105").Single()
            T = C105.TypeParameters(0)
            Assert.True(T.HasConstructorConstraint)
            Assert.True(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.None, T.Variance)

            Dim C106 = module0.GlobalNamespace.GetTypeMembers("C106").Single()
            T = C106.TypeParameters(0)
            Assert.True(T.HasConstructorConstraint)
            Assert.True(T.HasReferenceTypeConstraint)
            Assert.False(T.HasValueTypeConstraint)
            Assert.Equal(VarianceKind.Out, T.Variance)

            Dim I101 = module0.GlobalNamespace.GetTypeMembers("I101").Single()
            Dim I102 = module0.GlobalNamespace.GetTypeMembers("I102").Single()

            Dim C201 = module0.GlobalNamespace.GetTypeMembers("C201").Single()
            T = C201.TypeParameters(0)
            Assert.Equal(1, T.ConstraintTypes.Length)
            Assert.Same(I101, T.ConstraintTypes.ElementAt(0))

            Dim C202 = module0.GlobalNamespace.GetTypeMembers("C202").Single()
            T = C202.TypeParameters(0)
            Assert.Equal(2, T.ConstraintTypes.Length)
            Assert.Same(I101, T.ConstraintTypes.ElementAt(0))
            Assert.Same(I102, T.ConstraintTypes.ElementAt(1))

        End Sub

        <WorkItem(619267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619267")>
        <Fact>
        Public Sub InvalidNestedArity_2()
            Dim ilSource = <![CDATA[
.class interface public abstract I0
{
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public I0 { }
  }
}
.class interface public abstract IT<T>
{
  .class interface abstract nested public I0
  {
    .class interface abstract nested public I0 { }
    .class interface abstract nested public IT<T> { }
  }
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public I0 { }
  }
  .class interface abstract nested public ITU<T, U>
  {
    .class interface abstract nested public IT<T> { }
  }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C0_T
    Implements I0.IT(Of Object)
End Class
Class C0_T_0
    Implements I0.IT(Of Object).I0
End Class
Class CT_0
    Implements IT(Of Object).I0
End Class
Class CT_0_0
    Implements IT(Of Object).I0.I0
End Class
Class CT_0_T
    Implements IT(Of Object).I0.IT
End Class
Class CT_T_0
    Implements IT(Of Object).IT.I0
End Class
Class CT_TU_T
    Implements IT(Of Object).ITU(Of Integer).IT
End Class
]]>
                    </file>
                </compilation>
            Dim comp = CreateCompilationWithCustomILSource(vbSource, ilSource)
            comp.AssertTheseDiagnostics(<expected>
BC30649: 'I0.IT(Of T).I0' is an unsupported type.
    Implements I0.IT(Of Object).I0
               ~~~~~~~~~~~~~~~~~~~
BC30649: 'IT(Of T).I0' is an unsupported type.
    Implements IT(Of Object).I0
               ~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'IT(Of Object).I0.IT(Of T)'.
    Implements IT(Of Object).I0.IT
               ~~~~~~~~~~~~~~~~~~~
BC30649: 'IT(Of T).IT.I0' is an unsupported type.
    Implements IT(Of Object).IT.I0
               ~~~~~~~~~~~~~~~~~~~
BC30649: 'IT(Of T).ITU(Of U).IT' is an unsupported type.
    Implements IT(Of Object).ITU(Of Integer).IT
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class

End Namespace
