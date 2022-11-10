// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataHelpersTests
    {
        [Fact]
        public void IsValidMetadataIdentifier()
        {
            string lowSurrogate = "\uDC00";
            string highSurrogate = "\uD800";
            Assert.True(Char.IsLowSurrogate(lowSurrogate, 0));
            Assert.True(Char.IsHighSurrogate(highSurrogate, 0));
            Assert.True(Char.IsSurrogatePair(highSurrogate + lowSurrogate, 0));

            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(null));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(""));
            Assert.True(MetadataHelpers.IsValidMetadataIdentifier("x"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier("\0"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier("x\0"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier("\0x"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier("abc\0xyz\0uwq"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(lowSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(highSurrogate));
            Assert.True(MetadataHelpers.IsValidMetadataIdentifier(highSurrogate + lowSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(lowSurrogate + highSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(highSurrogate + "x" + lowSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(lowSurrogate + "x" + highSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(highSurrogate + "xxx"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(lowSurrogate + "xxx"));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(lowSurrogate + "\0" + highSurrogate));
            Assert.False(MetadataHelpers.IsValidMetadataIdentifier(highSurrogate + "\0" + lowSurrogate));
        }

        private enum ArrayKind
        {
            None,
            SzArray,
            SingleDimensional,
            MultiDimensional,
            JaggedSzArray,
            Jagged
        };

        private readonly struct TypeNameConfig
        {
            public readonly int NestingLevel;
            public readonly TypeNameConfig[] GenericParamsConfig;
            public readonly int PointerCount;
            public readonly ArrayKind ArrayKind;
            public readonly bool AssemblyQualified;

            public TypeNameConfig(int nestingLevel, TypeNameConfig[] genericParamsConfig, int pointerCount, ArrayKind arrayKind, bool assemblyQualified)
            {
                this.NestingLevel = nestingLevel;
                this.GenericParamsConfig = genericParamsConfig;
                this.PointerCount = pointerCount;
                this.ArrayKind = arrayKind;
                this.AssemblyQualified = assemblyQualified;
            }
        }

        private static TypeNameConfig[] GenerateTypeNameConfigs(int typeParamStackDepth)
        {
            var builder = ArrayBuilder<TypeNameConfig>.GetInstance();

            for (int nestingLevel = 0; nestingLevel <= 2; nestingLevel++)
            {
                for (int pointerCount = 0; pointerCount <= 2; pointerCount++)
                {
                    foreach (ArrayKind arrayKind in Enum.GetValues(typeof(ArrayKind)))
                    {
                        var genericParamsConfigBuilder = ArrayBuilder<TypeNameConfig[]>.GetInstance();
                        genericParamsConfigBuilder.Add(null);
                        if (typeParamStackDepth < 2)
                        {
                            genericParamsConfigBuilder.Add(GenerateTypeNameConfigs(typeParamStackDepth + 1));
                        }

                        foreach (var genericParamsConfig in genericParamsConfigBuilder.ToImmutableAndFree())
                        {
                            builder.Add(new TypeNameConfig(nestingLevel, genericParamsConfig, pointerCount, arrayKind, assemblyQualified: true));
                            builder.Add(new TypeNameConfig(nestingLevel, genericParamsConfig, pointerCount, arrayKind, assemblyQualified: false));
                        }
                    }
                }
            }

            return builder.ToArrayAndFree();
        }

        private static string[] GenerateTypeNamesToDecode(TypeNameConfig[] typeNameConfigs, out MetadataHelpers.AssemblyQualifiedTypeName[] expectedDecodeNames)
        {
            var pooledStrBuilder = PooledStringBuilder.GetInstance();
            StringBuilder typeNameBuilder = pooledStrBuilder.Builder;

            var typeNamesToDecode = new string[typeNameConfigs.Length];
            expectedDecodeNames = new MetadataHelpers.AssemblyQualifiedTypeName[typeNameConfigs.Length];

            for (int index = 0; index < typeNameConfigs.Length; index++)
            {
                TypeNameConfig typeNameConfig = typeNameConfigs[index];

                string expectedTopLevelTypeName = "X";
                typeNameBuilder.Append("X");

                string[] expectedNestedTypes = null;
                if (typeNameConfig.NestingLevel > 0)
                {
                    expectedNestedTypes = new string[typeNameConfig.NestingLevel];
                    for (int i = 0; i < typeNameConfig.NestingLevel; i++)
                    {
                        expectedNestedTypes[i] = "Y" + i;
                        typeNameBuilder.Append("+" + expectedNestedTypes[i]);
                    }
                }

                MetadataHelpers.AssemblyQualifiedTypeName[] expectedTypeArguments;
                if (typeNameConfig.GenericParamsConfig == null)
                {
                    expectedTypeArguments = null;
                }
                else
                {
                    string[] genericParamsToDecode = GenerateTypeNamesToDecode(typeNameConfig.GenericParamsConfig, out expectedTypeArguments);

                    var genericArityStr = "`" + genericParamsToDecode.Length.ToString();
                    typeNameBuilder.Append(genericArityStr);
                    if (typeNameConfig.NestingLevel == 0)
                    {
                        expectedTopLevelTypeName += genericArityStr;
                    }
                    else
                    {
                        expectedNestedTypes[typeNameConfig.NestingLevel - 1] += genericArityStr;
                    }

                    typeNameBuilder.Append("[");

                    for (int i = 0; i < genericParamsToDecode.Length; i++)
                    {
                        if (i > 0)
                        {
                            typeNameBuilder.Append(", ");
                        }

                        if (typeNameConfig.GenericParamsConfig[i].AssemblyQualified)
                        {
                            typeNameBuilder.Append("[");
                            typeNameBuilder.Append(genericParamsToDecode[i]);
                            typeNameBuilder.Append("]");
                        }
                        else
                        {
                            typeNameBuilder.Append(genericParamsToDecode[i]);
                        }
                    }

                    typeNameBuilder.Append("]");
                }

                int expectedPointerCount = typeNameConfig.PointerCount;
                typeNameBuilder.Append('*', expectedPointerCount);

                int[] expectedArrayRanks = null;
                switch (typeNameConfig.ArrayKind)
                {
                    case ArrayKind.SzArray:
                        typeNameBuilder.Append("[]");
                        expectedArrayRanks = new[] { 0 };
                        break;

                    case ArrayKind.SingleDimensional:
                        typeNameBuilder.Append("[*]");
                        expectedArrayRanks = new[] { 1 };
                        break;

                    case ArrayKind.MultiDimensional:
                        typeNameBuilder.Append("[,]");
                        expectedArrayRanks = new[] { 2 };
                        break;

                    case ArrayKind.JaggedSzArray:
                        typeNameBuilder.Append("[,][]");
                        expectedArrayRanks = new[] { 2, 0 };
                        break;

                    case ArrayKind.Jagged:
                        typeNameBuilder.Append("[,][*]");
                        expectedArrayRanks = new[] { 2, 1 };
                        break;
                }

                string expectedAssemblyName;
                if (typeNameConfig.AssemblyQualified)
                {
                    expectedAssemblyName = "Assembly, Version=0.0.0.0, Culture=neutral, null";
                    typeNameBuilder.Append(", " + expectedAssemblyName);
                }
                else
                {
                    expectedAssemblyName = null;
                }

                typeNamesToDecode[index] = typeNameBuilder.ToString();
                expectedDecodeNames[index] = new MetadataHelpers.AssemblyQualifiedTypeName(expectedTopLevelTypeName, expectedNestedTypes, expectedTypeArguments, expectedPointerCount, expectedArrayRanks, expectedAssemblyName);

                typeNameBuilder.Clear();
            }

            pooledStrBuilder.Free();
            return typeNamesToDecode;
        }

        private static void VerifyDecodedTypeName(
            MetadataHelpers.AssemblyQualifiedTypeName decodedName,
            string expectedTopLevelType,
            string expectedAssemblyName,
            string[] expectedNestedTypes,
            MetadataHelpers.AssemblyQualifiedTypeName[] expectedTypeArguments,
            int expectedPointerCount,
            int[] expectedArrayRanks)
        {
            Assert.Equal(expectedTopLevelType, decodedName.TopLevelType);
            Assert.Equal(expectedAssemblyName, decodedName.AssemblyName);
            Assert.Equal(expectedNestedTypes, decodedName.NestedTypes);

            if (decodedName.TypeArguments == null)
            {
                Assert.Null(expectedTypeArguments);
            }
            else
            {
                var decodedTypeArguments = decodedName.TypeArguments;
                for (int i = 0; i < decodedTypeArguments.Length; i++)
                {
                    var expectedTypeArgument = expectedTypeArguments[i];
                    VerifyDecodedTypeName(decodedTypeArguments[i], expectedTypeArgument.TopLevelType, expectedTypeArgument.AssemblyName,
                        expectedTypeArgument.NestedTypes, expectedTypeArgument.TypeArguments, expectedTypeArgument.PointerCount, expectedTypeArgument.ArrayRanks);
                }
            }

            Assert.Equal(expectedPointerCount, decodedName.PointerCount);
            AssertEx.Equal(expectedArrayRanks, decodedName.ArrayRanks);
        }

        private static void DecodeTypeNameAndVerify(
            string nameToDecode,
            string expectedTopLevelType,
            string expectedAssemblyName = null,
            string[] expectedNestedTypes = null,
            MetadataHelpers.AssemblyQualifiedTypeName[] expectedTypeArguments = null,
            int expectedPointerCount = 0,
            int[] expectedArrayRanks = null)
        {
            MetadataHelpers.AssemblyQualifiedTypeName decodedName = MetadataHelpers.DecodeTypeName(nameToDecode);
            VerifyDecodedTypeName(decodedName, expectedTopLevelType, expectedAssemblyName, expectedNestedTypes, expectedTypeArguments, expectedPointerCount, expectedArrayRanks);
        }

        private static void DecodeTypeNamesAndVerify(string[] namesToDecode, MetadataHelpers.AssemblyQualifiedTypeName[] expectedDecodedNames)
        {
            Assert.Equal(namesToDecode.Length, expectedDecodedNames.Length);

            for (int i = 0; i < namesToDecode.Length; i++)
            {
                var expectedDecodedName = expectedDecodedNames[i];
                DecodeTypeNameAndVerify(namesToDecode[i], expectedDecodedName.TopLevelType, expectedDecodedName.AssemblyName,
                    expectedDecodedName.NestedTypes, expectedDecodedName.TypeArguments, expectedDecodedName.PointerCount, expectedDecodedName.ArrayRanks);
            }
        }

        [WorkItem(546277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546277")]
        [Fact]
        public void TestDecodeTypeNameMatrix()
        {
            TypeNameConfig[] configsToTest = GenerateTypeNameConfigs(0);
            MetadataHelpers.AssemblyQualifiedTypeName[] expectedDecodedNames;
            string[] namesToDecode = GenerateTypeNamesToDecode(configsToTest, out expectedDecodedNames);
            DecodeTypeNamesAndVerify(namesToDecode, expectedDecodedNames);
        }

        [WorkItem(546277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546277")]
        [Fact]
        public void TestDecodeArrayTypeName_Bug15478()
        {
            DecodeTypeNameAndVerify("System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                expectedTopLevelType: "System.Int32",
                expectedAssemblyName: "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                expectedArrayRanks: new[] { 0 });
            DecodeTypeNameAndVerify("System.Int32[*], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                expectedTopLevelType: "System.Int32",
                expectedAssemblyName: "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                expectedArrayRanks: new[] { 1 });
        }

        [WorkItem(546277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546277")]
        [Fact]
        public void TestDecodeArrayTypeName_Valid()
        {
            // Single-D Array
            DecodeTypeNameAndVerify("W[]",
                expectedTopLevelType: "W",
                expectedArrayRanks: new[] { 0 });

            // Multi-D Array
            DecodeTypeNameAndVerify("W[,]",
                expectedTopLevelType: "W",
                expectedArrayRanks: new[] { 2 });

            // Jagged Array
            DecodeTypeNameAndVerify("W[][,]",
                expectedTopLevelType: "W",
                expectedArrayRanks: new[] { 0, 2 });

            // Generic Type Jagged Array
            DecodeTypeNameAndVerify("Y`1[W][][,]",
                expectedTopLevelType: "Y`1",
                expectedTypeArguments: new[] { new MetadataHelpers.AssemblyQualifiedTypeName("W", null, null, 0, null, null) },
                expectedArrayRanks: new[] { 0, 2 });

            // Nested Generic Type Jagged Array with Array type argument
            DecodeTypeNameAndVerify("Y`1+F[[System.Int32[], mscorlib]][,,][][,]",
                expectedTopLevelType: "Y`1",
                expectedNestedTypes: new[] { "F" },
                expectedTypeArguments: new[] { new MetadataHelpers.AssemblyQualifiedTypeName(
                                                    "System.Int32",
                                                    nestedTypes: null,
                                                    typeArguments: null,
                                                    pointerCount: 0,
                                                    arrayRanks: new[] { 0 },
                                                    assemblyName: "mscorlib") },
                expectedArrayRanks: new[] { 3, 0, 2 });

            // Nested Generic Type Jagged Array with type arguments from nested type and outer type
            DecodeTypeNameAndVerify("Y`1+Z`1[[System.Int32[], mscorlib], W][][,]",
                expectedTopLevelType: "Y`1",
                expectedNestedTypes: new[] { "Z`1" },
                expectedTypeArguments: new[] { new MetadataHelpers.AssemblyQualifiedTypeName(
                                                    "System.Int32",
                                                    nestedTypes: null,
                                                    typeArguments: null,
                                                    pointerCount: 0,
                                                    arrayRanks: new[] { 0 },
                                                    assemblyName: "mscorlib"),
                                               new MetadataHelpers.AssemblyQualifiedTypeName("W", null, null, 0, null, null) },
                expectedArrayRanks: new[] { 0, 2 });
        }

        [WorkItem(546277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546277")]
        [Fact]
        public void TestDecodeArrayTypeName_Invalid()
        {
            // Error case, array shape before nested type
            DecodeTypeNameAndVerify("X[]+Y",
                expectedTopLevelType: "X+Y",
                expectedNestedTypes: null,
                expectedArrayRanks: new[] { 0 });

            // Error case, array shape before generic type arguments
            DecodeTypeNameAndVerify("X[]`1[T]",
                expectedTopLevelType: "X`1[T]",
                expectedTypeArguments: null,
                expectedArrayRanks: new[] { 0 });

            // Error case, invalid array shape
            DecodeTypeNameAndVerify("X[T]",
                expectedTopLevelType: "X[T]",
                expectedTypeArguments: null,
                expectedArrayRanks: null);

            DecodeTypeNameAndVerify("X[,",
                expectedTopLevelType: "X[,",
                expectedTypeArguments: null,
                expectedArrayRanks: null);

            // Incomplete type argument assembly name
            DecodeTypeNameAndVerify("X`1[[T, Assembly",
                expectedTopLevelType: "X`1",
                expectedAssemblyName: null,
                expectedTypeArguments: new[] { new MetadataHelpers.AssemblyQualifiedTypeName("T", null, null, 0, null, "Assembly") },
                expectedArrayRanks: null);
        }

        [WorkItem(1140387, "DevDiv")]
        [Fact]
        public void TestDecodePointerType_Invalid()
        {
            // Error case, star before nested type
            DecodeTypeNameAndVerify("X*+Y",
                expectedTopLevelType: "X+Y",
                expectedNestedTypes: null,
                expectedPointerCount: 1);

            // Error case, star before generic type arguments
            DecodeTypeNameAndVerify("X*`1[T]",
                expectedTopLevelType: "X`1[T]",
                expectedTypeArguments: null,
                expectedPointerCount: 1);

            // Unsupported case, star after array shape
            // This is supported in the AQN grammar, but not in C# or VB.
            DecodeTypeNameAndVerify("W[]*",
                expectedTopLevelType: "W*",
                expectedTypeArguments: null,
                expectedPointerCount: 0,
                expectedArrayRanks: new[] { 0 });
        }

        [Fact, WorkItem(7396, "https://github.com/dotnet/roslyn/issues/7396")]
        public void ObfuscatedNamespaceNames_01()
        {
            var result = new ArrayBuilder<IGrouping<string, TypeDefinitionHandle>>();

            foreach (var namespaceName in new[] { "A.", "A.a", "A..", "A.-" })
            {
                result.Add(new Grouping<string, TypeDefinitionHandle>(namespaceName, new[] { new TypeDefinitionHandle() }));
            }

            result.Sort(new PEModule.TypesByNamespaceSortComparer(StringComparer.Ordinal));

            // This is equivalent to the result of PEModule.GroupTypesByNamespaceOrThrow
            IEnumerable<IGrouping<string, TypeDefinitionHandle>> typesByNS = result;

            // The following code is equivalent to code in PENamespaceSymbol.LoadAllMembers

            IEnumerable<IGrouping<string, TypeDefinitionHandle>> nestedTypes = null;
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>> nestedNamespaces = null;

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                false,
                "A".Length,
                typesByNS,
                StringComparer.Ordinal,
                out nestedTypes, out nestedNamespaces);

            // We don't expect duplicate keys in nestedNamespaces at this point.
            Assert.False(nestedNamespaces.GroupBy(pair => pair.Key).Where(g => g.Count() > 1).Any());

            var array = nestedNamespaces.ToArray();
            Assert.Equal(3, array.Length);
            Assert.Equal("", array[0].Key);
            Assert.Equal(2, array[0].Value.Count());
            Assert.Equal("-", array[1].Key);
            Assert.Equal(1, array[1].Value.Count());
            Assert.Equal("a", array[2].Key);
            Assert.Equal(1, array[2].Value.Count());
        }

        [Fact, WorkItem(7396, "https://github.com/dotnet/roslyn/issues/7396")]
        public void ObfuscatedNamespaceNames_02()
        {
            var result = new ArrayBuilder<IGrouping<string, TypeDefinitionHandle>>();

            foreach (var namespaceName in new[] { ".a", ".b" })
            {
                result.Add(new Grouping<string, TypeDefinitionHandle>(namespaceName, new[] { new TypeDefinitionHandle() }));
            }

            result.Sort(new PEModule.TypesByNamespaceSortComparer(StringComparer.Ordinal));

            // This is equivalent to the result of PEModule.GroupTypesByNamespaceOrThrow
            IEnumerable<IGrouping<string, TypeDefinitionHandle>> typesByNS = result;

            // The following code is equivalent to code in PENamespaceSymbol.LoadAllMembers

            IEnumerable<IGrouping<string, TypeDefinitionHandle>> nestedTypes = null;
            IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>> nestedNamespaces = null;

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                true, // global namespace
                0, // global namespace
                typesByNS,
                StringComparer.Ordinal,
                out nestedTypes, out nestedNamespaces);

            var nestedNS = nestedNamespaces.Single();

            Assert.Equal("", nestedNS.Key);
            Assert.Equal(2, nestedNS.Value.Count());

            MetadataHelpers.GetInfoForImmediateNamespaceMembers(
                false,
                nestedNS.Key.Length,
                nestedNS.Value,
                StringComparer.Ordinal,
                out nestedTypes, out nestedNamespaces);

            Assert.Equal(2, nestedNamespaces.Count());
            Assert.Equal("a", nestedNamespaces.ElementAt(0).Key);
            Assert.Equal("b", nestedNamespaces.ElementAt(1).Key);
        }
    }
}
