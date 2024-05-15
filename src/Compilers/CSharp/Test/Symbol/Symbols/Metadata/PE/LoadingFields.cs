// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingFields : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.SymbolsTests.Fields.CSFields.dll,
                TestReferences.SymbolsTests.Fields.VBFields.dll,
                TestMetadata.Net40.mscorlib
            },
            options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[1].Modules[0];
            var module3 = assemblies[2].Modules[0];

            var vbFields = module2.GlobalNamespace.GetTypeMembers("VBFields").Single();
            var csFields = module1.GlobalNamespace.GetTypeMembers("CSFields").Single();

            var f1 = (FieldSymbol)vbFields.GetMembers("F1").Single();
            var f2 = (FieldSymbol)vbFields.GetMembers("F2").Single();
            var f3 = (FieldSymbol)vbFields.GetMembers("F3").Single();
            var f4 = (FieldSymbol)vbFields.GetMembers("F4").Single();
            var f5 = (FieldSymbol)vbFields.GetMembers("F5").Single();
            var f6 = (FieldSymbol)csFields.GetMembers("F6").Single();

            Assert.Equal("F1", f1.Name);
            Assert.Same(vbFields.TypeParameters[0], f1.Type);
            Assert.False(f1.IsAbstract);
            Assert.False(f1.IsConst);
            Assert.True(f1.IsDefinition);
            Assert.False(f1.IsExtern);
            Assert.False(f1.IsOverride);
            Assert.False(f1.IsReadOnly);
            Assert.False(f1.IsSealed);
            Assert.True(f1.IsStatic);
            Assert.False(f1.IsVirtual);
            Assert.False(f1.IsVolatile);
            Assert.Equal(SymbolKind.Field, f1.Kind);
            Assert.Equal(module2.Locations, f1.Locations);
            Assert.Same(f1, f1.OriginalDefinition);
            Assert.Equal(Accessibility.Public, f1.DeclaredAccessibility);
            Assert.Same(vbFields, f1.ContainingSymbol);
            Assert.Equal(0, f1.TypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal("F2", f2.Name);
            Assert.Same(((PEModuleSymbol)module2).GetCorLibType(SpecialType.System_Int32), f2.Type);
            Assert.False(f2.IsConst);
            Assert.True(f2.IsReadOnly);
            Assert.False(f2.IsStatic);
            Assert.False(f2.IsVolatile);
            Assert.Equal(Accessibility.Protected, f2.DeclaredAccessibility);
            Assert.Equal(0, f2.TypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal("F3", f3.Name);
            Assert.False(f3.IsConst);
            Assert.False(f3.IsReadOnly);
            Assert.False(f3.IsStatic);
            Assert.False(f3.IsVolatile);
            Assert.Equal(Accessibility.Internal, f3.DeclaredAccessibility);
            Assert.Equal(0, f3.TypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal("F4", f4.Name);
            Assert.False(f4.IsConst);
            Assert.False(f4.IsReadOnly);
            Assert.False(f4.IsStatic);
            Assert.False(f4.IsVolatile);
            Assert.Equal(Accessibility.ProtectedOrInternal, f4.DeclaredAccessibility);
            Assert.Equal(0, f4.TypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal("F5", f5.Name);
            Assert.True(f5.IsConst);
            Assert.False(f5.IsReadOnly);
            Assert.True(f5.IsStatic);
            Assert.False(f5.IsVolatile);
            Assert.Equal(Accessibility.Protected, f5.DeclaredAccessibility);
            Assert.Equal(0, f5.TypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal("F6", f6.Name);
            Assert.False(f6.IsConst);
            Assert.False(f6.IsReadOnly);
            Assert.False(f6.IsStatic);
            Assert.True(f6.IsVolatile);
            Assert.Equal(1, f6.TypeWithAnnotations.CustomModifiers.Length);

            CustomModifier mod = f6.TypeWithAnnotations.CustomModifiers[0];

            Assert.False(mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsVolatile", mod.Modifier.ToTestDisplayString());

            Assert.Equal(SymbolKind.NamedType, csFields.GetMembers("FFF").Single().Kind);
            Assert.Equal(SymbolKind.Field, csFields.GetMembers("Fff").Single().Kind);
            Assert.Equal(SymbolKind.Method, csFields.GetMembers("FfF").Single().Kind);
        }

        [Fact]
        [WorkItem(193333, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=193333")]
        public void EnumWithPrivateValueField()
        {
            var il = @"
.class public auto ansi sealed TestEnum
       extends [mscorlib]System.Enum
{
  .field private specialname rtspecialname int32 value__
  .field public static literal valuetype TestEnum Value1 = int32(0x00000000)
  .field public static literal valuetype TestEnum Value2 = int32(0x00000001)
} // end of class TestEnum
";

            var text = @"
class Program
{
    static void Main()
    {
        TestEnum val = TestEnum.Value1;
        System.Console.WriteLine(val.ToString());
        val =  TestEnum.Value2;
        System.Console.WriteLine(val.ToString());
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(text, il, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"Value1
Value2");
        }

        [Fact]
        public void TestLoadFieldsOfReadOnlySpanFromCorlib()
        {
            var comp = CreateCompilation("", targetFramework: TargetFramework.Net60);

            var readOnlySpanType = comp.GetSpecialType(InternalSpecialType.System_ReadOnlySpan_T);
            Assert.False(readOnlySpanType.IsErrorType());
            Assert.Equal(SpecialType.None, readOnlySpanType.SpecialType);
            Assert.Equal((ExtendedSpecialType)InternalSpecialType.System_ReadOnlySpan_T, readOnlySpanType.ExtendedSpecialType);

            var fields = readOnlySpanType.GetMembers().OfType<FieldSymbol>();
            Assert.NotEmpty(fields);
        }
    }
}
