// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadCustomModifiers : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var modifiersModule = assemblies[0].Modules[0];

            var modifiers = modifiersModule.GlobalNamespace.GetTypeMembers("Modifiers").Single();

            FieldSymbol f0 = modifiers.GetMembers("F0").OfType<FieldSymbol>().Single();

            Assert.Equal(1, f0.TypeWithAnnotations.CustomModifiers.Length);

            var f0Mod = f0.TypeWithAnnotations.CustomModifiers[0];

            Assert.True(f0Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString());

            MethodSymbol m1 = modifiers.GetMembers("F1").OfType<MethodSymbol>().Single();
            ParameterSymbol p1 = m1.Parameters[0];
            ParameterSymbol p2 = modifiers.GetMembers("F2").OfType<MethodSymbol>().Single().Parameters[0];

            ParameterSymbol p4 = modifiers.GetMembers("F4").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m5 = modifiers.GetMembers("F5").OfType<MethodSymbol>().Single();
            ParameterSymbol p5 = m5.Parameters[0];

            ParameterSymbol p6 = modifiers.GetMembers("F6").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m7 = modifiers.GetMembers("F7").OfType<MethodSymbol>().Single();

            Assert.Equal(0, m1.ReturnTypeWithAnnotations.CustomModifiers.Length);

            Assert.Equal(1, p1.TypeWithAnnotations.CustomModifiers.Length);

            var p1Mod = p1.TypeWithAnnotations.CustomModifiers[0];

            Assert.True(p1Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString());

            Assert.Equal(2, p2.TypeWithAnnotations.CustomModifiers.Length);

            foreach (var p2Mod in p2.TypeWithAnnotations.CustomModifiers)
            {
                Assert.True(p2Mod.IsOptional);
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString());
            }

            Assert.Equal("System.Int32 modopt(System.Int32) modopt(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsConst) p", modifiers.GetMembers("F3").OfType<MethodSymbol>().Single().Parameters[0].ToTestDisplayString());

            Assert.Equal("System.Int32 modreq(System.Runtime.CompilerServices.IsConst) modopt(System.Runtime.CompilerServices.IsConst) p", p4.ToTestDisplayString());
            Assert.True(p4.HasUnsupportedMetadata);
            Assert.True(p4.ContainingSymbol.HasUnsupportedMetadata);

            Assert.True(m5.ReturnsVoid);
            Assert.Equal(1, m5.ReturnTypeWithAnnotations.CustomModifiers.Length);

            var m5Mod = m5.ReturnTypeWithAnnotations.CustomModifiers[0];
            Assert.True(m5Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p5.TypeWithAnnotations.CustomModifiers.Length);

            ArrayTypeSymbol p5Type = (ArrayTypeSymbol)p5.Type;

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString());

            Assert.Equal(1, p5Type.ElementTypeWithAnnotations.CustomModifiers.Length);
            var p5TypeMod = p5Type.ElementTypeWithAnnotations.CustomModifiers[0];

            Assert.True(p5TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p6.TypeWithAnnotations.CustomModifiers.Length);

            PointerTypeSymbol p6Type = (PointerTypeSymbol)p6.Type;

            Assert.Equal("System.Int32", p6Type.PointedAtType.ToTestDisplayString());

            Assert.Equal(1, p6Type.PointedAtTypeWithAnnotations.CustomModifiers.Length);
            var p6TypeMod = p6Type.PointedAtTypeWithAnnotations.CustomModifiers[0];

            Assert.True(p6TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p6TypeMod.Modifier.ToTestDisplayString());

            Assert.False(m7.ReturnsVoid);
            Assert.Equal(1, m7.ReturnTypeWithAnnotations.CustomModifiers.Length);

            var m7Mod = m7.ReturnTypeWithAnnotations.CustomModifiers[0];
            Assert.True(m7Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString());
        }

        [Fact]
        public void TestCustomModifierComparisons()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("Comparisons");

            var methods = @class.GetMembers("Method").Select(m => (MethodSymbol)m);
            Assert.Equal(19, methods.Count()); //sanity check that we got as many as we were expecting - change as needed

            //methods should be pairwise NotEqual since they all have different modopts
            foreach (var method1 in methods)
            {
                foreach (var method2 in methods)
                {
                    if (!ReferenceEquals(method1, method2))
                    {
                        //use a comparer that checks both return type and custom modifiers
                        Assert.False(MemberSignatureComparer.RuntimeImplicitImplementationComparer.Equals(method1, method2));
                    }
                }
            }
        }

        [Fact]
        public void TestPropertyTypeCustomModifiers()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");
            var property = @class.GetMember<PropertySymbol>("Property11");
            var propertyTypeCustomModifier = property.TypeWithAnnotations.CustomModifiers.Single();

            Assert.Equal("System.Runtime.CompilerServices.IsConst", propertyTypeCustomModifier.Modifier.ToTestDisplayString());
            Assert.True(propertyTypeCustomModifier.IsOptional);

            var propertyType = property.Type;
            Assert.Equal(TypeKind.Array, propertyType.TypeKind);

            var arrayPropertyType = (ArrayTypeSymbol)propertyType;
            var arrayPropertyTypeCustomModifiers = arrayPropertyType.ElementTypeWithAnnotations.CustomModifiers.Single();
            Assert.Equal("System.Runtime.CompilerServices.IsConst", arrayPropertyTypeCustomModifiers.Modifier.ToTestDisplayString());
            Assert.True(arrayPropertyTypeCustomModifiers.IsOptional);
        }

        [Fact]
        public void TestMethodCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("MethodCustomModifierCombinations");

            Assert.Equal(4, @class.GetMember<MethodSymbol>("Method1111").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1110").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1101").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1100").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method1011").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1010").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method1001").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method1000").CustomModifierCount());
            Assert.Equal(3, @class.GetMember<MethodSymbol>("Method0111").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0110").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0101").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0100").CustomModifierCount());
            Assert.Equal(2, @class.GetMember<MethodSymbol>("Method0011").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0010").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<MethodSymbol>("Method0001").CustomModifierCount());
            Assert.Equal(0, @class.GetMember<MethodSymbol>("Method0000").CustomModifierCount());
        }

        [Fact]
        public void TestPropertyCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");

            Assert.Equal(2, @class.GetMember<PropertySymbol>("Property11").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<PropertySymbol>("Property10").CustomModifierCount());
            Assert.Equal(1, @class.GetMember<PropertySymbol>("Property01").CustomModifierCount());
            Assert.Equal(0, @class.GetMember<PropertySymbol>("Property00").CustomModifierCount());
        }

        [Fact]
        public void TestEventCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                Net40.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("EventCustomModifierCombinations");

            Assert.True(@class.GetMember<EventSymbol>("Event11").Type.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(1, @class.GetMember<EventSymbol>("Event10").Type.CustomModifierCount());
            Assert.True(@class.GetMember<EventSymbol>("Event01").Type.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(0, @class.GetMember<EventSymbol>("Event00").Type.CustomModifierCount());
        }

        [Fact]
        public void TestFieldCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                    Net40.mscorlib
                });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("FieldCustomModifierCombinations");

            Assert.Equal(2, CustomModifierCount(@class.GetMember<FieldSymbol>("field11")));
            Assert.Equal(1, CustomModifierCount(@class.GetMember<FieldSymbol>("field10")));
            Assert.Equal(1, CustomModifierCount(@class.GetMember<FieldSymbol>("field01")));
            Assert.Equal(0, CustomModifierCount(@class.GetMember<FieldSymbol>("field00")));
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the type
        /// of the specified field.
        /// </summary>
        internal static int CustomModifierCount(FieldSymbol field)
        {
            int count = 0;

            count += field.TypeWithAnnotations.CustomModifiers.Length;
            count += field.Type.CustomModifierCount();

            return count;
        }
    }
}
