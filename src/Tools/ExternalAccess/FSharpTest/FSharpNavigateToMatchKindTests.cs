// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{
    public class FSharpNavigateToMatchKindTests
    {
        public static IEnumerable<object[]> enumValues()
        {
            foreach (var number in Enum.GetValues(typeof(FSharpNavigateToMatchKind)))
            {
                yield return new object[] { number };
            }
        }

        internal static NavigateToMatchKind GetExpectedNavigateToMatchKind(FSharpNavigateToMatchKind kind)
        {
            switch (kind)
            {
                case FSharpNavigateToMatchKind.Exact:
                    {
                        return NavigateToMatchKind.Exact;
                    }
                case FSharpNavigateToMatchKind.Prefix:
                    {
                        return NavigateToMatchKind.Prefix;
                    }
                case FSharpNavigateToMatchKind.Substring:
                    {
                        return NavigateToMatchKind.Substring;
                    }
                case FSharpNavigateToMatchKind.Regular:
                    {
                        return NavigateToMatchKind.Regular;
                    }
                case FSharpNavigateToMatchKind.None:
                    {
                        return NavigateToMatchKind.None;
                    }
                case FSharpNavigateToMatchKind.CamelCaseExact:
                    {
                        return NavigateToMatchKind.CamelCaseExact;
                    }
                case FSharpNavigateToMatchKind.CamelCasePrefix:
                    {
                        return NavigateToMatchKind.CamelCasePrefix;
                    }
                case FSharpNavigateToMatchKind.CamelCaseNonContiguousPrefix:
                    {
                        return NavigateToMatchKind.CamelCaseNonContiguousPrefix;
                    }
                case FSharpNavigateToMatchKind.CamelCaseSubstring:
                    {
                        return NavigateToMatchKind.CamelCaseSubstring;
                    }
                case FSharpNavigateToMatchKind.CamelCaseNonContiguousSubstring:
                    {
                        return NavigateToMatchKind.CamelCaseNonContiguousSubstring;
                    }
                case FSharpNavigateToMatchKind.Fuzzy:
                    {
                        return NavigateToMatchKind.Fuzzy;
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(kind);
                    }
            }
        }

        [Theory]
        [MemberData(nameof(enumValues))]
        internal void MapsCorrectly(FSharpNavigateToMatchKind kind)
        {
            var actual = FSharpNavigateToMatchKindHelpers.ConvertTo(kind);
            var expected = GetExpectedNavigateToMatchKind(kind);
            Assert.Equal(expected, actual);
        }
    }
}
