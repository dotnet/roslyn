' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingFields : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                             {
                                TestResources.SymbolsTests.Fields.CSFields,
                                TestResources.SymbolsTests.Fields.VBFields,
                                TestMetadata.ResourcesNet40.mscorlib
                             }, importInternals:=True)

            Dim module1 = assemblies(0).Modules(0)
            Dim module2 = assemblies(1).Modules(0)
            Dim module3 = assemblies(2).Modules(0)

            Dim vbFields = module2.GlobalNamespace.GetTypeMembers("VBFields").Single()
            Dim csFields = module1.GlobalNamespace.GetTypeMembers("CSFields").Single()

            Dim f1 = DirectCast(vbFields.GetMembers("F1").Single(), FieldSymbol)
            Dim f2 = DirectCast(vbFields.GetMembers("F2").Single(), FieldSymbol)
            Dim f3 = DirectCast(vbFields.GetMembers("F3").Single(), FieldSymbol)
            Dim f4 = DirectCast(vbFields.GetMembers("F4").Single(), FieldSymbol)
            Dim f5 = DirectCast(vbFields.GetMembers("F5").Single(), FieldSymbol)
            Dim f6 = DirectCast(csFields.GetMembers("F6").Single(), FieldSymbol)

            Assert.Equal("F1", f1.Name)
            Assert.Same(vbFields.TypeParameters(0), f1.Type)
            Assert.False(f1.IsMustOverride)
            Assert.False(f1.IsConst)
            Assert.True(f1.IsDefinition)
            Assert.False(f1.IsOverrides)
            Assert.False(f1.IsReadOnly)
            Assert.False(f1.IsNotOverridable)
            Assert.True(f1.IsShared)
            Assert.False(f1.IsOverridable)
            Assert.Equal(SymbolKind.Field, f1.Kind)
            Assert.Equal(module2.Locations, f1.Locations)
            Assert.Same(f1, f1.OriginalDefinition)
            Assert.Equal(Accessibility.Public, f1.DeclaredAccessibility)
            Assert.Same(vbFields, f1.ContainingSymbol)
            Assert.Equal(0, f1.CustomModifiers.Length)

            Assert.Equal("F2", f2.Name)
            Assert.Same(DirectCast(module2, PEModuleSymbol).GetCorLibType(SpecialType.System_Int32), f2.Type)
            Assert.False(f2.IsConst)
            Assert.True(f2.IsReadOnly)
            Assert.False(f2.IsShared)
            Assert.Equal(Accessibility.Protected, f2.DeclaredAccessibility)
            Assert.Equal(0, f2.CustomModifiers.Length)

            Assert.Equal("F3", f3.Name)
            Assert.False(f3.IsConst)
            Assert.False(f3.IsReadOnly)
            Assert.False(f3.IsShared)
            Assert.Equal(Accessibility.Friend, f3.DeclaredAccessibility)
            Assert.Equal(0, f3.CustomModifiers.Length)

            Assert.Equal("F4", f4.Name)
            Assert.False(f4.IsConst)
            Assert.False(f4.IsReadOnly)
            Assert.False(f4.IsShared)
            Assert.Equal(Accessibility.ProtectedOrFriend, f4.DeclaredAccessibility)
            Assert.Equal(0, f4.CustomModifiers.Length)

            Assert.Equal("F5", f5.Name)
            Assert.True(f5.IsConst)
            Assert.False(f5.IsReadOnly)
            Assert.True(f5.IsShared)
            Assert.Equal(Accessibility.Protected, f5.DeclaredAccessibility)
            Assert.Equal(0, f5.CustomModifiers.Length)

            Assert.Equal("F6", f6.Name)
            Assert.False(f6.IsConst)
            Assert.False(f6.IsReadOnly)
            Assert.False(f6.IsShared)
            Assert.False(f6.CustomModifiers.Single().IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsVolatile", f6.CustomModifiers.Single().Modifier.ToTestDisplayString())
            Assert.True(f6.HasUnsupportedMetadata)

            Assert.Equal(3, csFields.GetMembers("FFF").Length())
            Assert.Equal(3, csFields.GetMembers("Fff").Length())
            Assert.Equal(3, csFields.GetMembers("FfF").Length())
        End Sub

        <Fact>
        Public Sub ConstantFields()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                             {
                                TestResources.SymbolsTests.Fields.ConstantFields,
                                TestMetadata.ResourcesNet40.mscorlib
                             })

            Dim module1 = assemblies(0).Modules(0)

            Dim ConstFields = module1.GlobalNamespace.GetTypeMembers("ConstFields").Single()

            Dim ByteEnum = module1.GlobalNamespace.GetTypeMembers("ByteEnum").Single()
            Dim SByteEnum = module1.GlobalNamespace.GetTypeMembers("SByteEnum").Single()
            Dim UInt16Enum = module1.GlobalNamespace.GetTypeMembers("UInt16Enum").Single()
            Dim Int16Enum = module1.GlobalNamespace.GetTypeMembers("Int16Enum").Single()
            Dim UInt32Enum = module1.GlobalNamespace.GetTypeMembers("UInt32Enum").Single()
            Dim Int32Enum = module1.GlobalNamespace.GetTypeMembers("Int32Enum").Single()
            Dim UInt64Enum = module1.GlobalNamespace.GetTypeMembers("UInt64Enum").Single()
            Dim Int64Enum = module1.GlobalNamespace.GetTypeMembers("Int64Enum").Single()

            'Public Const Int64Field As Long = 634315546432909307
            'Public DateTimeField As DateTime
            'Public Const SingleField As Single = 9
            'Public Const DoubleField As Double = -10
            'Public Const StringField As String = "11"
            'Public Const StringNullField As String = Nothing
            'Public Const ObjectNullField As Object = Nothing

            Dim Int64Field = DirectCast(ConstFields.GetMembers("Int64Field").Single(), FieldSymbol)
            Dim DateTimeField = DirectCast(ConstFields.GetMembers("DateTimeField").Single(), FieldSymbol)
            Dim SingleField = DirectCast(ConstFields.GetMembers("SingleField").Single(), FieldSymbol)
            Dim DoubleField = DirectCast(ConstFields.GetMembers("DoubleField").Single(), FieldSymbol)
            Dim StringField = DirectCast(ConstFields.GetMembers("StringField").Single(), FieldSymbol)
            Dim StringNullField = DirectCast(ConstFields.GetMembers("StringNullField").Single(), FieldSymbol)
            Dim ObjectNullField = DirectCast(ConstFields.GetMembers("ObjectNullField").Single(), FieldSymbol)

            Assert.True(Int64Field.IsConst)
            Assert.True(Int64Field.HasConstantValue)
            Assert.Equal(Int64Field.ConstantValue, 634315546432909307)
            Assert.Equal(ConstantValueTypeDiscriminator.Int64, Int64Field.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(634315546432909307, Int64Field.GetConstantValue(ConstantFieldsInProgress.Empty).Int64Value)

            Assert.True(DateTimeField.IsConst)
            Assert.True(DateTimeField.HasConstantValue)
            Assert.Equal(DateTimeField.ConstantValue, New DateTime(634315546432909307))
            Assert.Equal(ConstantValueTypeDiscriminator.DateTime, DateTimeField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(New DateTime(634315546432909307), DateTimeField.GetConstantValue(ConstantFieldsInProgress.Empty).DateTimeValue)

            Assert.True(SingleField.IsConst)
            Assert.True(SingleField.HasConstantValue)
            Assert.Equal(SingleField.ConstantValue, 9.0F)
            Assert.Equal(ConstantValueTypeDiscriminator.Single, SingleField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(9.0F, SingleField.GetConstantValue(ConstantFieldsInProgress.Empty).SingleValue)

            Assert.True(DoubleField.IsConst)
            Assert.True(DoubleField.HasConstantValue)
            Assert.Equal(DoubleField.ConstantValue, -10.0)
            Assert.Equal(ConstantValueTypeDiscriminator.Double, DoubleField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(-10.0, DoubleField.GetConstantValue(ConstantFieldsInProgress.Empty).DoubleValue)

            Assert.True(StringField.IsConst)
            Assert.True(StringField.HasConstantValue)
            Assert.Equal(StringField.ConstantValue, "11")
            Assert.Equal(ConstantValueTypeDiscriminator.String, StringField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal("11", StringField.GetConstantValue(ConstantFieldsInProgress.Empty).StringValue)

            Assert.True(StringNullField.IsConst)
            Assert.True(StringNullField.HasConstantValue)
            Assert.Null(StringNullField.ConstantValue)
            Assert.Equal(ConstantValueTypeDiscriminator.Nothing, StringNullField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)

            Assert.True(ObjectNullField.IsConst)
            Assert.True(ObjectNullField.HasConstantValue)
            Assert.Null(ObjectNullField.ConstantValue)
            Assert.Equal(ConstantValueTypeDiscriminator.Nothing, ObjectNullField.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)

            'ByteValue = 1
            'SByteValue = -2
            'UInt16Value = 3
            'Int16Value = -4
            'UInt32Value = 5
            'Int32Value = -6
            'UInt64Value = 7
            'Int64Value = -8

            Dim ByteValue = DirectCast(ByteEnum.GetMembers("ByteValue").Single(), FieldSymbol)
            Dim SByteValue = DirectCast(SByteEnum.GetMembers("SByteValue").Single(), FieldSymbol)
            Dim UInt16Value = DirectCast(UInt16Enum.GetMembers("UInt16Value").Single(), FieldSymbol)
            Dim Int16Value = DirectCast(Int16Enum.GetMembers("Int16Value").Single(), FieldSymbol)
            Dim UInt32Value = DirectCast(UInt32Enum.GetMembers("UInt32Value").Single(), FieldSymbol)
            Dim Int32Value = DirectCast(Int32Enum.GetMembers("Int32Value").Single(), FieldSymbol)
            Dim UInt64Value = DirectCast(UInt64Enum.GetMembers("UInt64Value").Single(), FieldSymbol)
            Dim Int64Value = DirectCast(Int64Enum.GetMembers("Int64Value").Single(), FieldSymbol)

            Assert.True(ByteValue.IsConst)
            Assert.True(ByteValue.HasConstantValue)
            Assert.Equal(ByteValue.ConstantValue, CByte(1))
            Assert.Equal(ConstantValueTypeDiscriminator.Byte, ByteValue.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(CByte(1), ByteValue.GetConstantValue(ConstantFieldsInProgress.Empty).ByteValue)

            Assert.True(SByteValue.IsConst)
            Assert.True(SByteValue.HasConstantValue)
            Assert.Equal(SByteValue.ConstantValue, CSByte(-2))
            Assert.Equal(ConstantValueTypeDiscriminator.SByte, SByteValue.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(CSByte(-2), SByteValue.GetConstantValue(ConstantFieldsInProgress.Empty).SByteValue)

            Assert.True(UInt16Value.IsConst)
            Assert.True(UInt16Value.HasConstantValue)
            Assert.Equal(UInt16Value.ConstantValue, 3US)
            Assert.Equal(ConstantValueTypeDiscriminator.UInt16, UInt16Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(3US, UInt16Value.GetConstantValue(ConstantFieldsInProgress.Empty).UInt16Value)

            Assert.True(Int16Value.IsConst)
            Assert.True(Int16Value.HasConstantValue)
            Assert.Equal(Int16Value.ConstantValue, -4S)
            Assert.Equal(ConstantValueTypeDiscriminator.Int16, Int16Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(-4S, Int16Value.GetConstantValue(ConstantFieldsInProgress.Empty).Int16Value)

            Assert.True(UInt32Value.IsConst)
            Assert.True(UInt32Value.HasConstantValue)
            Assert.Equal(UInt32Value.ConstantValue, 5UI)
            Assert.Equal(ConstantValueTypeDiscriminator.UInt32, UInt32Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(5UI, UInt32Value.GetConstantValue(ConstantFieldsInProgress.Empty).UInt32Value)

            Assert.True(Int32Value.IsConst)
            Assert.True(Int32Value.HasConstantValue)
            Assert.Equal(Int32Value.ConstantValue, -6)
            Assert.Equal(ConstantValueTypeDiscriminator.Int32, Int32Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(-6, Int32Value.GetConstantValue(ConstantFieldsInProgress.Empty).Int32Value)

            Assert.True(UInt64Value.IsConst)
            Assert.True(UInt64Value.HasConstantValue)
            Assert.Equal(UInt64Value.ConstantValue, 7UL)
            Assert.Equal(ConstantValueTypeDiscriminator.UInt64, UInt64Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(7UL, UInt64Value.GetConstantValue(ConstantFieldsInProgress.Empty).UInt64Value)

            Assert.True(Int64Value.IsConst)
            Assert.True(Int64Value.HasConstantValue)
            Assert.Equal(Int64Value.ConstantValue, -8L)
            Assert.Equal(ConstantValueTypeDiscriminator.Int64, Int64Value.GetConstantValue(ConstantFieldsInProgress.Empty).Discriminator)
            Assert.Equal(-8L, Int64Value.GetConstantValue(ConstantFieldsInProgress.Empty).Int64Value)
        End Sub

        <Fact>
        <WorkItem(193333, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=193333")>
        Public Sub EnumWithPrivateValueField()

            Dim ilSource = "
.class public auto ansi sealed TestEnum
       extends [mscorlib]System.Enum
{
  .field private specialname rtspecialname int32 value__
  .field public static literal valuetype TestEnum Value1 = int32(0x00000000)
  .field public static literal valuetype TestEnum Value2 = int32(0x00000001)
} // end of class TestEnum
"

            Dim vbSource =
<compilation>
    <file>
Module Module1
    Sub Main()
        Dim val as TestEnum = TestEnum.Value1
        System.Console.WriteLine(val.ToString())
        val =  TestEnum.Value2
        System.Console.WriteLine(val.ToString())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, includeVbRuntime:=True, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="Value1
Value2")
        End Sub

    End Class

End Namespace
