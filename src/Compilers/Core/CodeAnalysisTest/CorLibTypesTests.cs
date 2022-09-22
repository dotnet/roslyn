// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CorLibTypesAndConstantTests : TestBase
    {
        [Fact]
        public void IntegrityTest()
        {
            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                string name = SpecialTypes.GetMetadataName((SpecialType)i);
                Assert.Equal((SpecialType)i, SpecialTypes.GetTypeFromMetadataName(name));
            }

            for (int i = 0; i <= (int)SpecialType.Count; i++)
            {
                Cci.PrimitiveTypeCode code = SpecialTypes.GetTypeCode((SpecialType)i);

                if (code != Cci.PrimitiveTypeCode.NotPrimitive)
                {
                    Assert.Equal((SpecialType)i, SpecialTypes.GetTypeFromMetadataName(code));
                }
            }

            for (int i = 0; i <= (int)Cci.PrimitiveTypeCode.Invalid; i++)
            {
                SpecialType id = SpecialTypes.GetTypeFromMetadataName((Cci.PrimitiveTypeCode)i);

                if (id != SpecialType.None)
                {
                    Assert.Equal((Cci.PrimitiveTypeCode)i, SpecialTypes.GetTypeCode(id));
                }
            }

            Assert.Equal(SpecialType.System_Boolean, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Boolean));
            Assert.Equal(SpecialType.System_Char, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Char));
            Assert.Equal(SpecialType.System_Void, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Void));
            Assert.Equal(SpecialType.System_String, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.String));
            Assert.Equal(SpecialType.System_Int64, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Int64));
            Assert.Equal(SpecialType.System_Int32, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Int32));
            Assert.Equal(SpecialType.System_Int16, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Int16));
            Assert.Equal(SpecialType.System_SByte, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Int8));
            Assert.Equal(SpecialType.System_UInt64, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.UInt64));
            Assert.Equal(SpecialType.System_UInt32, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.UInt32));
            Assert.Equal(SpecialType.System_UInt16, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.UInt16));
            Assert.Equal(SpecialType.System_Byte, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.UInt8));
            Assert.Equal(SpecialType.System_Single, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Float32));
            Assert.Equal(SpecialType.System_Double, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.Float64));
            Assert.Equal(SpecialType.System_IntPtr, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.IntPtr));
            Assert.Equal(SpecialType.System_UIntPtr, SpecialTypes.GetTypeFromMetadataName(Cci.PrimitiveTypeCode.UIntPtr));
        }

        [Fact]
        public void SpecialTypeIsValueType()
        {
            var comp = CSharp.CSharpCompilation.Create(
                "c",
                options: new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel),
                references: new[] { NetCoreApp.SystemRuntime });

            var knownMissingTypes = new HashSet<SpecialType>()
            {
            };

            for (var specialType = SpecialType.None + 1; specialType <= SpecialType.Count; specialType++)
            {
                var symbol = comp.GetSpecialType(specialType);
                if (knownMissingTypes.Contains(specialType))
                {
                    Assert.Equal(SymbolKind.ErrorType, symbol.Kind);
                }
                else
                {
                    Assert.NotEqual(SymbolKind.ErrorType, symbol.Kind);
                    Assert.Equal(symbol.IsValueType, specialType.IsValueType());
                }
            }
        }

        [Fact]
        public void ConstantValueInvalidOperationTest01()
        {
            Assert.Throws<InvalidOperationException>(() => { ConstantValue.Create(null, ConstantValueTypeDiscriminator.Bad); });

            var cv = ConstantValue.Create(1);
            Assert.Throws<InvalidOperationException>(() => { var c = cv.StringValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv.CharValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv.DateTimeValue; });

            var cv1 = ConstantValue.Create(null, ConstantValueTypeDiscriminator.Null);
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.BooleanValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.DecimalValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.DoubleValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.SingleValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.SByteValue; });
            Assert.Throws<InvalidOperationException>(() => { var c = cv1.ByteValue; });
        }

        [Fact]
        public void ConstantValuePropertiesTest01()
        {
            Assert.Equal(ConstantValue.Bad, ConstantValue.Default(ConstantValueTypeDiscriminator.Bad));

            var cv1 = ConstantValue.Create((sbyte)-1);
            Assert.True(cv1.IsNegativeNumeric);

            var cv2 = ConstantValue.Create(-0.12345f);
            Assert.True(cv2.IsNegativeNumeric);

            var cv3 = ConstantValue.Create((double)-1.234);
            Assert.True(cv3.IsNegativeNumeric);

            var cv4 = ConstantValue.Create((decimal)-12345m);
            Assert.True(cv4.IsNegativeNumeric);
            Assert.False(cv4.IsDateTime);

            var cv5 = ConstantValue.Create(null);
            Assert.False(cv5.IsNumeric);
            Assert.False(cv5.IsBoolean);
            Assert.False(cv5.IsFloating);
        }

        [Fact]
        public void ConstantValueGetHashCodeTest01()
        {
            var cv11 = ConstantValue.Create((sbyte)-1);
            var cv12 = ConstantValue.Create((sbyte)-1);
            Assert.Equal(cv11.GetHashCode(), cv12.GetHashCode());

            var cv21 = ConstantValue.Create((byte)255);
            var cv22 = ConstantValue.Create((byte)255);
            Assert.Equal(cv21.GetHashCode(), cv22.GetHashCode());

            var cv31 = ConstantValue.Create((short)-32768);
            var cv32 = ConstantValue.Create((short)-32768);
            Assert.Equal(cv31.GetHashCode(), cv32.GetHashCode());

            var cv41 = ConstantValue.Create((ushort)65535);
            var cv42 = ConstantValue.Create((ushort)65535);
            Assert.Equal(cv41.GetHashCode(), cv42.GetHashCode());
            // int
            var cv51 = ConstantValue.Create(12345);
            var cv52 = ConstantValue.Create(12345);
            Assert.Equal(cv51.GetHashCode(), cv52.GetHashCode());

            var cv61 = ConstantValue.Create(uint.MinValue);
            var cv62 = ConstantValue.Create(uint.MinValue);
            Assert.Equal(cv61.GetHashCode(), cv62.GetHashCode());

            var cv71 = ConstantValue.Create(long.MaxValue);
            var cv72 = ConstantValue.Create(long.MaxValue);
            Assert.Equal(cv71.GetHashCode(), cv72.GetHashCode());

            var cv81 = ConstantValue.Create((ulong)123456789);
            var cv82 = ConstantValue.Create((ulong)123456789);
            Assert.Equal(cv81.GetHashCode(), cv82.GetHashCode());

            var cv91 = ConstantValue.Create(1.1m);
            var cv92 = ConstantValue.Create(1.1m);
            Assert.Equal(cv91.GetHashCode(), cv92.GetHashCode());
        }

        // In general, different values are not required to have different hash codes.
        // But for perf reasons we want hash functions with a good distribution, 
        // so we expect hash codes to differ if a single component is incremented.
        // But program correctness should be preserved even with a null hash function,
        // so we need a way to disable these tests during such correctness validation.
#if !DISABLE_GOOD_HASH_TESTS

        [Fact]
        public void ConstantValueGetHashCodeTest02()
        {
            var cv1 = ConstantValue.Create("1");
            var cv2 = ConstantValue.Create("2");

            Assert.NotEqual(cv1.GetHashCode(), cv2.GetHashCode());
        }

#endif

        [Fact]
        public void ConstantValueToStringTest01()
        {
            var value = "Null";
#if NETCOREAPP
            value = "Nothing";
#endif

            var cv = ConstantValue.Create(null, ConstantValueTypeDiscriminator.Null);
            Assert.Equal($"ConstantValueNull(null: {value})", cv.ToString());

            cv = ConstantValue.Create(null, ConstantValueTypeDiscriminator.String);
            Assert.Equal($"ConstantValueNull(null: {value})", cv.ToString());

            // Never hit "ConstantValueString(null: Null)"

            var strVal = "QC";
            cv = ConstantValue.Create(strVal);
            Assert.Equal(@"ConstantValueString(""QC"": String)", cv.ToString());

            cv = ConstantValue.Create((sbyte)-128);
            Assert.Equal("ConstantValueI8(-128: SByte)", cv.ToString());

            cv = ConstantValue.Create((ulong)123456789);
            Assert.Equal("ConstantValueI64(123456789: UInt64)", cv.ToString());
        }
    }
}
