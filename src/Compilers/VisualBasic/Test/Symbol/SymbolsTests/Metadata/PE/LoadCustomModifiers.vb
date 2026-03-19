' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.CSharp
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadCustomModifiers : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                             {
                                TestResources.SymbolsTests.CustomModifiers.Modifiers,
                                Net40.Resources.mscorlib
                             })

            Dim modifiersModule = assemblies(0).Modules(0)

            Dim modifiers = modifiersModule.GlobalNamespace.GetTypeMembers("Modifiers").Single()

            Dim f0 = modifiers.GetMembers("F0").OfType(Of FieldSymbol)().Single()

            Assert.Equal(1, f0.CustomModifiers.Length)

            Dim f0Mod = f0.CustomModifiers(0)

            Assert.True(f0Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString())

            Dim m1 As MethodSymbol = modifiers.GetMembers("F1").OfType(Of MethodSymbol)().Single()
            Dim p1 As ParameterSymbol = m1.Parameters(0)
            Dim p2 As ParameterSymbol = modifiers.GetMembers("F2").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim p4 As ParameterSymbol = modifiers.GetMembers("F4").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim m5 As MethodSymbol = modifiers.GetMembers("F5").OfType(Of MethodSymbol)().Single()
            Dim p5 As ParameterSymbol = m5.Parameters(0)

            Dim p6 As ParameterSymbol = modifiers.GetMembers("F6").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim m7 As MethodSymbol = modifiers.GetMembers("F7").OfType(Of MethodSymbol)().Single()

            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length)

            Assert.Equal(1, p1.CustomModifiers.Length)

            Dim p1Mod = p1.CustomModifiers(0)

            Assert.True(p1Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString())

            Assert.Equal(2, p2.CustomModifiers.Length)

            For Each p2Mod In p2.CustomModifiers
                Assert.True(p2Mod.IsOptional)
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString())
            Next

            Assert.Equal("p As System.Int32 modopt(System.Int32) modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsConst)", modifiers.GetMembers("F3").OfType(Of MethodSymbol)().Single().Parameters(0).ToTestDisplayString())

            Assert.Equal("p As System.Int32 modreq(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsConst)", p4.ToTestDisplayString())
            Assert.True(p4.HasUnsupportedMetadata)
            Assert.True(p4.ContainingSymbol.HasUnsupportedMetadata)

            Assert.True(m5.IsSub)
            Assert.Equal(1, m5.ReturnTypeCustomModifiers.Length)

            Dim m5Mod = m5.ReturnTypeCustomModifiers(0)
            Assert.True(m5Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString())

            Assert.Equal(0, p5.CustomModifiers.Length)

            Dim p5Type As ArrayTypeSymbol = DirectCast(p5.Type, ArrayTypeSymbol)

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString())

            Assert.Equal(1, p5Type.CustomModifiers.Length)
            Dim p5TypeMod = p5Type.CustomModifiers(0)

            Assert.True(p5TypeMod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString())

            Assert.Equal(0, p6.CustomModifiers.Length)

            Dim p6Type As TypeSymbol = p6.Type

            Assert.IsType(Of PointerTypeSymbol)(p6Type)
            Assert.Equal(ERRID.ERR_UnsupportedType1, p6Type.GetUseSiteErrorInfo().Code)

            Assert.False(m7.IsSub)
            Assert.Equal(1, m7.ReturnTypeCustomModifiers.Length)

            Dim m7Mod = m7.ReturnTypeCustomModifiers(0)
            Assert.True(m7Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub UnmanagedConstraint_OnClass()
            Dim reference = CreateCSharpCompilation("
public class TestRef<T> where T : unmanaged
{
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = New TestRef(Of String)()
        Dim y = New TestRef(Of Integer)()
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference})

            Dim errs As XElement =
<expected>
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim x = New TestRef(Of String)()
                               ~~~~~~
BC37332: Type argument 'String' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        Dim x = New TestRef(Of String)()
                               ~~~~~~
</expected>

            AssertTheseDiagnostics(compilation, errs)

            Dim typeParameter = compilation.GetTypeByMetadataName("TestRef`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)

            typeParameter = compilation.GetTypeByMetadataName("System.Nullable`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.False(typeParameter.HasUnmanagedTypeConstraint)

            compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference}, parseOptions:=TestOptions.Regular17_13)
            AssertTheseDiagnostics(compilation, errs)

            typeParameter = compilation.GetTypeByMetadataName("TestRef`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)
            compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference}, parseOptions:=TestOptions.RegularLatest)

            AssertTheseDiagnostics(compilation, errs)

            typeParameter = compilation.GetTypeByMetadataName("TestRef`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)

            compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference}, parseOptions:=TestOptions.Regular16_9)
            AssertTheseDiagnostics(compilation, <expected>
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
        Dim x = New TestRef(Of String)()
                               ~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        Dim x = New TestRef(Of String)()
                               ~~~~~~
BC37332: Type argument 'String' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        Dim x = New TestRef(Of String)()
                               ~~~~~~
BC36716: Visual Basic 16.9 does not support recognizing 'unmanaged' constraint.
        Dim y = New TestRef(Of Integer)()
                               ~~~~~~~
                                                </expected>)

            typeParameter = compilation.GetTypeByMetadataName("TestRef`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraint_OnMethod()
            Dim reference = CreateCSharpCompilation("
public class TestRef
{
    public void M<T>() where T : unmanaged
    {
    }
}", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main() 
        Dim x = New TestRef()
        x.M(Of String)()
    End Sub
End Class
    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
        x.M(Of String)()
          ~~~~~~~~~~~~
BC37332: Type argument 'String' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
        x.M(Of String)()
          ~~~~~~~~~~~~
                                                </expected>)

            Dim typeParameter = compilation.GetTypeByMetadataName("TestRef").GetMethod("M").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)
        End Sub

        <Fact>
        Public Sub UnmanagedConstraint_OnDelegate()
            Dim reference = CreateCSharpCompilation("
public delegate T D<T>() where T : unmanaged;
", parseOptions:=New CSharpParseOptions(CSharp.LanguageVersion.Latest)).EmitToImageReference()

            Dim source =
                <compilation>
                    <file>
Class Test
    Shared Sub Main(del As D(Of String)) 
    End Sub
End Class
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={reference})

            AssertTheseDiagnostics(compilation, <expected>
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Shared Sub Main(del As D(Of String)) 
                    ~~~
BC37332: Type argument 'String' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as type parameter 'T'.
    Shared Sub Main(del As D(Of String)) 
                    ~~~
                                                </expected>)

            Dim typeParameter = compilation.GetTypeByMetadataName("D`1").TypeParameters.Single()
            Assert.True(typeParameter.HasValueTypeConstraint)
            Assert.True(typeParameter.HasUnmanagedTypeConstraint)
        End Sub

    End Class

End Namespace
