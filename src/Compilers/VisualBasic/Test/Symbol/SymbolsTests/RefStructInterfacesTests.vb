' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RefStructInterfacesTests
        Inherits BasicTestBase

        Private Shared ReadOnly s_targetFrameworkSupportingByRefLikeGenerics As TargetFramework = TargetFramework.Net90

        <Fact>
        Public Sub RuntimeCapability_01()

            Dim comp = CreateCompilation("", targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics)
            Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.ByRefLikeGenerics))

            comp = CreateCompilation("", targetFramework:=TargetFramework.Mscorlib461Extended)
            Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.ByRefLikeGenerics))

            comp = CreateCompilation("", targetFramework:=TargetFramework.Net80)
            Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.ByRefLikeGenerics))
        End Sub

        <Fact>
        Public Sub AllowsConstraint_01()

            Dim csSource =
"
public class A<T>;

public class C<T>
    where T : allows ref struct
{
}

public class D
{
    static public void M<T>() where T : allows ref struct
    {
        System.Console.WriteLine(typeof(T));
    }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(s_targetFrameworkSupportingByRefLikeGenerics)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class B(Of S)
    Inherits C(Of S)
End Class

Public Class Test
    Shared Sub Main()
        Dim c1 = New C(Of Integer)()
        Dim c2 = New C(Of Test)()

        Dim b1 = New B(Of Integer)()
        Dim b2 = New B(Of Test)()

        D.M(Of Integer)()
        D.M(Of Test)()

        System.Console.Write("Done")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics, references:={csCompilation}, options:=TestOptions.DebugExe)

            Dim a = comp1.GetTypeByMetadataName("A`1")
            AssertEx.Equal("A(Of T)", a.ToDisplayString(SymbolDisplayFormat.TestFormatWithConstraints))
            Assert.False(a.TypeParameters.Single().AllowsRefLikeType)

            Dim b = comp1.GetTypeByMetadataName("B`1")
            AssertEx.Equal("B(Of S)", b.ToDisplayString(SymbolDisplayFormat.TestFormatWithConstraints))
            Assert.False(b.TypeParameters.Single().AllowsRefLikeType)

            Dim c = comp1.GetTypeByMetadataName("C`1")
            AssertEx.Equal("C(Of T)", c.ToDisplayString(SymbolDisplayFormat.TestFormatWithConstraints))
            Assert.True(c.TypeParameters.Single().AllowsRefLikeType)

            c = b.BaseTypeNoUseSiteDiagnostics
            AssertEx.Equal("C(Of S)", c.ToDisplayString(SymbolDisplayFormat.TestFormatWithConstraints))
            Assert.True(c.TypeParameters.Single().AllowsRefLikeType)

            CompileAndVerify(
                comp1,
                verify:=If(ExecutionConditionUtil.IsMonoOrCoreClr, Verification.Passes, Verification.Skipped),
                expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"System.Int32
Test
Done", Nothing)).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AllowsConstraint_02()

            Dim csSource =
"
public class C
{
    public virtual void M<T>()
        where T : allows ref struct
    {
    }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(s_targetFrameworkSupportingByRefLikeGenerics)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class B
    Inherits C

    Public Overrides Sub M(Of T)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics, references:={csCompilation})

            Dim m = comp1.GetMember(Of MethodSymbol)("B.M")
            Assert.False(m.TypeParameters.Single().AllowsRefLikeType)
            Assert.True(m.OverriddenMethod.TypeParameters.Single().AllowsRefLikeType)

            comp1.AssertTheseDiagnostics(
<expected>
BC32077: 'Public Overrides Sub M(Of T)()' cannot override 'Public Overridable Overloads Sub M(Of T)()' because they differ by type parameter constraints.
    Public Overrides Sub M(Of T)
                         ~
</expected>)
        End Sub

        <Fact>
        Public Sub AllowsConstraint_03()

            Dim csSource =
"
public interface IC
{
    public abstract void M<T>()
        where T : allows ref struct;
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(s_targetFrameworkSupportingByRefLikeGenerics)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class B
    Implements IC

    Sub M(Of T) Implements IC.M
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC32078: 'Public Sub M(Of T)()' cannot implement 'IC.Sub M(Of T)()' because they differ by type parameter constraints.
    Sub M(Of T) Implements IC.M
                           ~~~~
</expected>)

            Dim m = comp1.GetMember(Of MethodSymbol)("B.M")
            Assert.False(m.TypeParameters.Single().AllowsRefLikeType)
            Assert.True(m.ExplicitInterfaceImplementations.Single().TypeParameters.Single().AllowsRefLikeType)
        End Sub

        <Fact()>
        Public Sub NoPiaEmbedding()
            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public interface ITest29
{
    void M21<T1>() where T1 : allows ref struct;
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           assemblyName:="Pia",
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(s_targetFrameworkSupportingByRefLikeGenerics)).EmitToImageReference(embedInteropTypes:=True)

            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Interface I3
    Inherits ITest29
End Interface
]]></file>
                           </compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()

                                                           Dim type2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest29")
                                                           Assert.Equal(TypeKind.Interface, type2.TypeKind)
                                                           Dim method = type2.GetMember(Of PEMethodSymbol)("M21")
                                                           Dim tp = method.TypeParameters
                                                           Dim t1 = tp(0)
                                                           Assert.Equal("T1", t1.Name)
                                                           Assert.True(t1.AllowsRefLikeType)
                                                       End Sub

            Dim compilation1 = CreateCompilation(
                sources1,
                targetFramework:=s_targetFrameworkSupportingByRefLikeGenerics,
                options:=TestOptions.DebugDll,
                references:={csCompilation})

            CompileAndVerify(compilation1, symbolValidator:=validator, verify:=Verification.Skipped).VerifyDiagnostics()
        End Sub
    End Class
End Namespace

