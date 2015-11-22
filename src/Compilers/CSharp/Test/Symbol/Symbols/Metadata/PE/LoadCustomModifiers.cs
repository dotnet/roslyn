// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

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
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var modifiersModule = assemblies[0].Modules[0];


            var modifiers = modifiersModule.GlobalNamespace.GetTypeMembers("Modifiers").Single();

            FieldSymbol f0 = modifiers.GetMembers("F0").OfType<FieldSymbol>().Single();

            Assert.Equal(1, f0.Type.CustomModifiers.Length);

            var f0Mod = f0.Type.CustomModifiers[0];

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

            Assert.Equal(0, m1.ReturnType.CustomModifiers.Length);

            Assert.Equal(1, p1.Type.CustomModifiers.Length);

            var p1Mod = p1.Type.CustomModifiers[0];

            Assert.True(p1Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString());

            Assert.Equal(2, p2.Type.CustomModifiers.Length);

            foreach (var p2Mod in p2.Type.CustomModifiers)
            {
                Assert.True(p2Mod.IsOptional);
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString());
            }

            Assert.Equal(SymbolKind.ErrorType, p4.Type.Kind);

            Assert.True(m5.ReturnsVoid);
            Assert.Equal(1, m5.ReturnType.CustomModifiers.Length);

            var m5Mod = m5.ReturnType.CustomModifiers[0];
            Assert.True(m5Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p5.Type.CustomModifiers.Length);

            ArrayTypeSymbol p5Type = (ArrayTypeSymbol)p5.Type.TypeSymbol;

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString());

            Assert.Equal(1, p5Type.ElementType.CustomModifiers.Length);
            var p5TypeMod = p5Type.ElementType.CustomModifiers[0];

            Assert.True(p5TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString());

            Assert.Equal(0, p6.Type.CustomModifiers.Length);

            PointerTypeSymbol p6Type = (PointerTypeSymbol)p6.Type.TypeSymbol;

            Assert.Equal("System.Int32", p6Type.PointedAtType.ToTestDisplayString());

            Assert.Equal(1, p6Type.PointedAtType.CustomModifiers.Length);
            var p6TypeMod = p6Type.PointedAtType.CustomModifiers[0];

            Assert.True(p6TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p6TypeMod.Modifier.ToTestDisplayString());

            Assert.False(m7.ReturnsVoid);
            Assert.Equal(1, m7.ReturnType.CustomModifiers.Length);

            var m7Mod = m7.ReturnType.CustomModifiers[0];
            Assert.True(m7Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString());
        }

        [Fact]
        public void TestCustomModifierComparisons()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
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
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");
            var property = @class.GetMember<PropertySymbol>("Property11");
            var propertyTypeCustomModifier = property.Type.CustomModifiers.Single();

            Assert.Equal("System.Runtime.CompilerServices.IsConst", propertyTypeCustomModifier.Modifier.ToTestDisplayString());
            Assert.True(propertyTypeCustomModifier.IsOptional);

            var propertyType = property.Type.TypeSymbol;
            Assert.Equal(TypeKind.Array, propertyType.TypeKind);

            var arrayPropertyType = (ArrayTypeSymbol)propertyType;
            var arrayPropertyTypeCustomModifiers = arrayPropertyType.ElementType.CustomModifiers.Single();
            Assert.Equal("System.Runtime.CompilerServices.IsConst", arrayPropertyTypeCustomModifiers.Modifier.ToTestDisplayString());
            Assert.True(arrayPropertyTypeCustomModifiers.IsOptional);
        }

        [Fact]
        public void TestMethodCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
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
                TestReferences.NetFx.v4_0_21006.mscorlib
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
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("EventCustomModifierCombinations");

            Assert.True(@class.GetMember<EventSymbol>("Event11").Type.TypeSymbol.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(1, @class.GetMember<EventSymbol>("Event10").Type.TypeSymbol.CustomModifierCount());
            Assert.True(@class.GetMember<EventSymbol>("Event01").Type.TypeSymbol.IsErrorType()); //Can't have modopt on event type
            Assert.Equal(0, @class.GetMember<EventSymbol>("Event00").Type.TypeSymbol.CustomModifierCount());
        }

        [Fact]
        public void TestFieldCustomModifierCount()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                    TestReferences.NetFx.v4_0_21006.mscorlib
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

            count += field.Type.CustomModifiers.Length;
            count += field.Type.TypeSymbol.CustomModifierCount();

            return count;
        }
    }
}
