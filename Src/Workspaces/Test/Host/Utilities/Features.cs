// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal static partial class Features
    {
        public static class All
        {
            /// <summary>
            /// All LanguageServices and LanguageServiceProviders in Roslyn (Both C# and VB).
            /// </summary>
            public static ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> LanguageServices
            {
                get
                {
                    return allLanguageServices.Value;
                }
            }

            /// <summary>
            ///  All FormattingRules in Roslyn (Both C# and VB).
            /// </summary>
            public static ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>> FormattingRules
            {
                get
                {
                    return allFormattingRules.Value;
                }
            }

            private static Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>> allLanguageServices =
                new Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>>(() => CSharp.Services.AddRange(VisualBasic.Services), true);

            private static Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>> allFormattingRules =
                new Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>>(() => CSharp.FormattingRules.AddRange(VisualBasic.FormattingRules), true);
        }
    }
}
