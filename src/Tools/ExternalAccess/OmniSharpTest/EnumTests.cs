﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractInterface;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.ImplementType;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.UnitTests
{
    public class EnumTests
    {
        [Theory]
        [InlineData(typeof(ExtractInterfaceOptionsResult.ExtractLocation),
                    typeof(OmniSharpExtractInterfaceOptionsResult.OmniSharpExtractLocation))]
        [InlineData(typeof(ImplementTypeInsertionBehavior), typeof(OmniSharpImplementTypeInsertionBehavior))]
        [InlineData(typeof(ImplementTypePropertyGenerationBehavior), typeof(OmniSharpImplementTypePropertyGenerationBehavior))]
        public void AssertEnumsInSync(Type internalType, Type externalType)
        {
            var internalValues = Enum.GetValues(internalType).Cast<int>().ToArray();
            var internalNames = Enum.GetNames(internalType);
            var externalValues = Enum.GetValues(externalType).Cast<int>().ToArray();
            var externalNames = Enum.GetNames(externalType);

            Assert.Equal(internalValues, externalValues);
            Assert.Equal(internalNames, externalNames);
        }
    }
}
