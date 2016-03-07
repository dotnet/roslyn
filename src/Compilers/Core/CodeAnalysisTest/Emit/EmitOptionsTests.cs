// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EmitOptionsTests : TestBase
    {
        private void TestProperty<T>(
            Func<EmitOptions, T, EmitOptions> factory,
            Func<EmitOptions, T> getter,
            T validNonDefaultValue)
        {
            var oldOpt1 = EmitOptions.Default;

            var validDefaultValue = getter(oldOpt1);

            // we need non-default value to test Equals and GetHashCode
            Assert.NotEqual(validNonDefaultValue, validDefaultValue);

            // check that the assigned value can be read:
            var newOpt1 = factory(oldOpt1, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(newOpt1));

            // check that creating new options with the same value yields the same options instance:
            var newOpt1_alias = factory(newOpt1, validNonDefaultValue);
            Assert.Same(newOpt1_alias, newOpt1);

            // check that Equals and GetHashCode work
            var newOpt2 = factory(oldOpt1, validNonDefaultValue);
            Assert.False(newOpt1.Equals(oldOpt1));
            Assert.True(newOpt1.Equals(newOpt2));

            Assert.Equal(newOpt1.GetHashCode(), newOpt2.GetHashCode());

            // test default(T):
            Assert.NotNull(factory(oldOpt1, default(T)));
        }

        [Fact]
        public void WithXxx()
        {
            TestProperty((old, value) => old.WithFileAlignment(value), opt => opt.FileAlignment, 2048);
            TestProperty((old, value) => old.WithBaseAddress(value), opt => opt.BaseAddress, 100UL);
            TestProperty((old, value) => old.WithHighEntropyVirtualAddressSpace(value), opt => opt.HighEntropyVirtualAddressSpace, true);
            TestProperty((old, value) => old.WithSubsystemVersion(value), opt => opt.SubsystemVersion, SubsystemVersion.Windows2000);
            TestProperty((old, value) => old.WithRuntimeMetadataVersion(value), opt => opt.RuntimeMetadataVersion, "v12345");
            TestProperty((old, value) => old.WithPdbFilePath(value), opt => opt.PdbFilePath, @"c:\temp\a.pdb");
            TestProperty((old, value) => old.WithOutputNameOverride(value), opt => opt.OutputNameOverride, @"x.dll");
            TestProperty((old, value) => old.WithDebugInformationFormat(value), opt => opt.DebugInformationFormat, (DebugInformationFormat)2);
            TestProperty((old, value) => old.WithTolerateErrors(value), opt => opt.TolerateErrors, true);
            TestProperty((old, value) => old.WithIncludePrivateMembers(value), opt => opt.IncludePrivateMembers, true);
        }

        /// <summary>
        /// If this test fails, please update the <see cref="EmitOptions.GetHashCode"/>
        /// and <see cref="EmitOptions.Equals(EmitOptions)"/> methods to
        /// make sure they are doing the right thing with your new field and then update the baseline
        /// here.
        /// </summary>
        [Fact]
        public void TestFieldsForEqualsAndGetHashCode()
        {
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                typeof(EmitOptions),
                "EmitDynamicAnalysisData",
                "EmitMetadataOnly",
                "SubsystemVersion",
                "FileAlignment",
                "HighEntropyVirtualAddressSpace",
                "BaseAddress",
                "DebugInformationFormat",
                "OutputNameOverride",
                "PdbFilePath",
                "RuntimeMetadataVersion",
                "TolerateErrors",
                "IncludePrivateMembers");
        }
    }
}
