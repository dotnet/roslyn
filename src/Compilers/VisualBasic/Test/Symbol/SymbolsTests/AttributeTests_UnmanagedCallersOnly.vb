' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class AttributeTests_UnmanagedCallersOnly
        Inherits BasicTestBase

        Private ReadOnly _parseOptions As CSharp.CSharpParseOptions = CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Default)
        Private ReadOnly _csharpCompOptions As CSharp.CSharpCompilationOptions = New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe:=True)
        Private ReadOnly _csharpReferences As ImmutableArray(Of MetadataReference) = TargetFrameworkUtil.GetReferences(TargetFramework.Net50)

        Private ReadOnly UnmanagedCallersOnlyAttributeIl As String = <![CDATA[
.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 40 00 00 00 01 00 54 02 09 49 6e 68 65 72
        69 74 65 64 00
    )
    .field public class [mscorlib]System.Type[] CallConvs
    .field public string EntryPoint

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    }
}
]]>.Value

        <Fact>
        Public Sub UnmanagedCallersOnlyInSource()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Class C
    <UnmanagedCallersOnly>
    Sub S1()
        S1()
    End Sub

    <UnmanagedCallersOnly(CallConvs := { GetType(CallConvCdecl) })>
    Sub S2()
        S2()
    End Sub

    Property P1 As String
        <UnmanagedCallersOnly>
        Get
            Return ""
        End Get
        <UnmanagedCallersOnly>
        Set
        End Set
    End Property

    Sub S3()
        P1 = P1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilation(source, targetFramework:=TargetFramework.Net50)

            comp.AssertTheseDiagnostics(<![CDATA[
BC37316: 'UnmanagedCallersOnly' attribute is not supported.
    <UnmanagedCallersOnly>
     ~~~~~~~~~~~~~~~~~~~~
BC37316: 'UnmanagedCallersOnly' attribute is not supported.
    <UnmanagedCallersOnly(CallConvs := { GetType(CallConvCdecl) })>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37316: 'UnmanagedCallersOnly' attribute is not supported.
        <UnmanagedCallersOnly>
         ~~~~~~~~~~~~~~~~~~~~
BC37316: 'UnmanagedCallersOnly' attribute is not supported.
        <UnmanagedCallersOnly>
         ~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyInCsharp()
            Dim source0 = <![CDATA[
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly]
    public static void M1() { }
}
]]>

            Dim comp0 = CreateCSharpCompilation(source0, parseOptions:=_parseOptions, compilationOptions:=_csharpCompOptions, referencedAssemblies:=_csharpReferences)
            comp0.VerifyDiagnostics()

            Dim reference = comp0.EmitToImageReference()

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1()
        C.M1()
        Dim f As System.Action = AddressOf C.M1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilation(source, references:={reference}, targetFramework:=TargetFramework.Net50)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: 'M1' has a return type that is not supported or parameter types that are not supported.
        C.M1()
          ~~
BC30657: 'M1' has a return type that is not supported or parameter types that are not supported.
        Dim f As System.Action = AddressOf C.M1
                                           ~~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim m1 = c.GetMethod("M1")

            Assert.NotNull(m1.GetUseSiteErrorInfo())
            Assert.True(m1.HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyOnProperty()
            Dim il = UnmanagedCallersOnlyAttributeIl + <![CDATA[
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig specialname static 
        int32 get_Prop () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .method public hidebysig specialname static 
        void set_Prop (
            int32 'value'
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .property int32 Prop()
    {
        .get int32 C::get_Prop()
        .set void C::set_Prop(int32)
    }
}
]]>.Value

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1()
        C.Prop = 1
        Dim i = C.Prop
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: 'Prop' has a return type that is not supported or parameter types that are not supported.
        C.Prop = 1
        ~~~~~~
BC30657: 'Prop' has a return type that is not supported or parameter types that are not supported.
        Dim i = C.Prop
                ~~~~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim prop = c.GetProperty("Prop")

            Assert.NotNull(prop.GetMethod.GetUseSiteErrorInfo())
            Assert.True(prop.GetMethod.HasUnsupportedMetadata)
            Assert.NotNull(prop.SetMethod.GetUseSiteErrorInfo())
            Assert.True(prop.SetMethod.HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyOnInstanceMethod()
            Dim il = UnmanagedCallersOnlyAttributeIl + <![CDATA[
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig 
        instance void M1 () cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ret
    }
}
]]>.Value

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1(c1 As C)
        c1.M1()
        Dim f As System.Action = AddressOf c1.M1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: 'M1' has a return type that is not supported or parameter types that are not supported.
        c1.M1()
           ~~
BC30657: 'M1' has a return type that is not supported or parameter types that are not supported.
        Dim f As System.Action = AddressOf c1.M1
                                           ~~~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim m1 = c.GetMethod("M1")

            Assert.NotNull(m1.GetUseSiteErrorInfo())
            Assert.True(m1.HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyOnIndexer()
            Dim il = UnmanagedCallersOnlyAttributeIl + <![CDATA[
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )
    // Methods
    .method public hidebysig specialname 
        instance void set_Item (
            int32 i,
            int32 'value'
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        nop
        ret
    } // end of method C::set_Item

    .method public hidebysig specialname 
        instance int32 get_Item (
            int32 i
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    } // end of method C::get_Item

    // Properties
    .property instance int32 Item(
        int32 i
    )
    {
        .get instance int32 C::get_Item(int32)
        .set instance void C::set_Item(int32, int32)
    }
}
]]>.Value

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1(c1 As C)
        c1(1) = 1
        Dim i = c1(1)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: 'Item' has a return type that is not supported or parameter types that are not supported.
        c1(1) = 1
        ~~~~~
BC30657: 'Item' has a return type that is not supported or parameter types that are not supported.
        Dim i = c1(1)
                ~~~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim prop = c.GetProperty("Item")

            Assert.NotNull(prop.GetMethod.GetUseSiteErrorInfo())
            Assert.True(prop.GetMethod.HasUnsupportedMetadata)
            Assert.NotNull(prop.SetMethod.GetUseSiteErrorInfo())
            Assert.True(prop.SetMethod.HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyOnCustomBinaryOperator()
            Dim il = UnmanagedCallersOnlyAttributeIl + <![CDATA[
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        class C op_Addition (
            class C c1,
            class C c2
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        ret
    }
}
]]>.Value

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1(c1 As C, c2 As C)
        Dim c3 = c1 + c2
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: '+' has a return type that is not supported or parameter types that are not supported.
        Dim c3 = c1 + c2
                 ~~~~~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim add = c.GetMethod("op_Addition")

            Assert.NotNull(add.GetUseSiteErrorInfo())
            Assert.True(add.HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub UnmanagedCallersOnlyOnCustomUnaryOperator()
            Dim il = UnmanagedCallersOnlyAttributeIl + <![CDATA[
.class public auto ansi beforefieldinit C
    extends [mscorlib]System.Object
{
    .method public hidebysig specialname static 
        class C op_UnaryPlus (
            class C c1
        ) cil managed 
    {
        // [System.Runtime.InteropServices.UnmanagedCallersOnly]
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        ret
    }
}
]]>.Value

            Dim source =
<compilation>
    <file><![CDATA[
Class D
    Sub S1(c1 As C)
        Dim c2 = +c1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)

            comp.AssertTheseDiagnostics(<![CDATA[
BC30657: '+' has a return type that is not supported or parameter types that are not supported.
        Dim c2 = +c1
                 ~~~
]]>)

            Dim c = comp.GetTypeByMetadataName("C")
            Dim plus = c.GetMethod("op_UnaryPlus")

            Assert.NotNull(plus.GetUseSiteErrorInfo())
            Assert.True(plus.HasUnsupportedMetadata)
        End Sub
    End Class
End Namespace
