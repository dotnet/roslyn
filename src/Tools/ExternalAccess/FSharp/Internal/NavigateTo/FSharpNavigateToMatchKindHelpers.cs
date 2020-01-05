// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo
{
    internal static class FSharpNavigateToMatchKindHelpers
    {
        public static NavigateToMatchKind ConvertTo(FSharpNavigateToMatchKind kind)
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
    }
}
