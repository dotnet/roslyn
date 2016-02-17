// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DynamicFlagsCustomTypeInfoTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void BoolArrayConstructor()
        {
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(new bool[0]));

            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false), 0x00);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(true), 0x01);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false), 0x00);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(true, false), 0x01);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, true), 0x02);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(true, true), 0x03);

            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, true), 0x04);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, true), 0x08);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, false, true), 0x10);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, false, false, true), 0x20);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, false, false, false, true), 0x40);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, false, false, false, false, true), 0x80);
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(false, false, false, false, false, false, false, false, true), 0x00, 0x01);
        }

        [Fact]
        public void CustomTypeInfoConstructor()
        {
            ValidateCustomTypeInfo();

            ValidateCustomTypeInfo(0x00);
            ValidateCustomTypeInfo(0x01);
            ValidateCustomTypeInfo(0x02);
            ValidateCustomTypeInfo(0x03);

            ValidateCustomTypeInfo(0x04);
            ValidateCustomTypeInfo(0x08);
            ValidateCustomTypeInfo(0x10);
            ValidateCustomTypeInfo(0x20);
            ValidateCustomTypeInfo(0x40);
            ValidateCustomTypeInfo(0x80);
            ValidateCustomTypeInfo(0x00, 0x01);
        }

        [Fact]
        public void CustomTypeInfoConstructor_OtherGuid()
        {
            ValidateBytes(DynamicFlagsCustomTypeInfo.Create(DkmClrCustomTypeInfo.Create(Guid.NewGuid(), new ReadOnlyCollection<byte>(new byte[] { 0x01 }))));
        }

        [Fact]
        public void Indexer()
        {
            ValidateIndexer(null);
            ValidateIndexer(false);
            ValidateIndexer(true);
            ValidateIndexer(false, false);
            ValidateIndexer(false, true);
            ValidateIndexer(true, false);
            ValidateIndexer(true, true);

            ValidateIndexer(false, false, true);
            ValidateIndexer(false, false, false, true);
            ValidateIndexer(false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, false, false, true);
        }

        [Fact]
        public void Any()
        {
            ValidateAny(null);
            ValidateAny(false);
            ValidateAny(true);
            ValidateAny(false, false);
            ValidateAny(false, true);
            ValidateAny(true, false);
            ValidateAny(true, true);

            ValidateAny(false, false, true);
            ValidateAny(false, false, false, true);
            ValidateAny(false, false, false, false, true);
            ValidateAny(false, false, false, false, false, true);
            ValidateAny(false, false, false, false, false, false, true);
            ValidateAny(false, false, false, false, false, false, false, true);
            ValidateAny(false, false, false, false, false, false, false, false, true);
        }

        [Fact]
        public void SkipOne()
        {
            var dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.Create((bool[])null);

            ValidateBytes(dynamicFlagsCustomTypeInfo.SkipOne());

            var dkmClrCustomTypeInfo = DkmClrCustomTypeInfo.Create(DynamicFlagsCustomTypeInfo.PayloadTypeId, new ReadOnlyCollection<byte>(new byte[] { 0x80 }));
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.Create(dkmClrCustomTypeInfo);

            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x40);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x20);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x10);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x08);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x04);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x02);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x01);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x00);

            dkmClrCustomTypeInfo = DkmClrCustomTypeInfo.Create(DynamicFlagsCustomTypeInfo.PayloadTypeId, new ReadOnlyCollection<byte>(new byte[] { 0x00, 0x02 }));
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.Create(dkmClrCustomTypeInfo);

            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x00, 0x01);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x80, 0x00);
            dynamicFlagsCustomTypeInfo = dynamicFlagsCustomTypeInfo.SkipOne();
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x40, 0x00);
        }

        private static void ValidateCustomTypeInfo(params byte[] payload)
        {
            Assert.NotNull(payload);

            var dkmClrCustomTypeInfo = DkmClrCustomTypeInfo.Create(DynamicFlagsCustomTypeInfo.PayloadTypeId, new ReadOnlyCollection<byte>(payload));
            var dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.Create(dkmClrCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, payload);

            var dkmClrCustomTypeInfo2 = dynamicFlagsCustomTypeInfo.GetCustomTypeInfo();
            if (dynamicFlagsCustomTypeInfo.Any())
            {
                Assert.Equal(dkmClrCustomTypeInfo.PayloadTypeId, dkmClrCustomTypeInfo2.PayloadTypeId);
                Assert.Equal(dkmClrCustomTypeInfo.Payload, dkmClrCustomTypeInfo2.Payload);
            }
            else
            {
                Assert.Null(dkmClrCustomTypeInfo2);
            }
        }

        private static void ValidateIndexer(params bool[] flags)
        {
            var customTypeInfo = DynamicFlagsCustomTypeInfo.Create(flags);
            if (flags == null)
            {
                Assert.False(customTypeInfo[0]);
            }
            else
            {
                AssertEx.All(flags.Select((f, i) => f == customTypeInfo[i]), x => x);
                Assert.False(customTypeInfo[flags.Length]);
            }
        }

        private static void ValidateAny(params bool[] flags)
        {
            var customTypeInfo = DynamicFlagsCustomTypeInfo.Create(flags);
            if (flags == null)
            {
                Assert.False(customTypeInfo.Any());
            }
            else
            {
                Assert.Equal(flags.Any(x => x), customTypeInfo.Any());
            }
        }

        private static void ValidateBytes(DynamicFlagsCustomTypeInfo dynamicFlags, params byte[] expectedBytes)
        {
            Assert.NotNull(expectedBytes);

            var dkmClrCustomTypeInfo = dynamicFlags.GetCustomTypeInfo();
            if (dynamicFlags.Any())
            {
                Assert.NotNull(dkmClrCustomTypeInfo);
                var actualBytes = dkmClrCustomTypeInfo.Payload;
                Assert.Equal(expectedBytes, actualBytes);
            }
            else
            {
                AssertEx.All(expectedBytes, b => b == 0);
                Assert.Null(dkmClrCustomTypeInfo);
            }
        }
    }
}