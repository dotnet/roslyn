// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerConfigCodingConventionsContext : ICodingConventionContext, ICodingConventionsSnapshot
    {
        private readonly AnalyzerConfigOptions _analyzerConfigOptions;

        public AnalyzerConfigCodingConventionsContext(AnalyzerConfigOptions analyzerConfigOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
        }

        public ICodingConventionsSnapshot CurrentConventions => this;

        bool ICodingConventionsSnapshot.TryGetConventionValue<T>(string conventionName, [MaybeNullWhen(returnValue: false)] out T conventionValue)
        {
            if (typeof(T) != typeof(string))
            {
                conventionValue = default!;
                return false;
            }

            if (_analyzerConfigOptions.TryGetValue(conventionName, out var value))
            {
                conventionValue = (T)(object)value;
            }
            else
            {
                conventionValue = default!;
            }

            return conventionValue is object;
        }
    }
}
