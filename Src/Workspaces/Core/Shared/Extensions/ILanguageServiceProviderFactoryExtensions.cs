// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ILanguageServiceProviderFactoryExtensions
    {
        public static TService GetService<TService>(this ILanguageServiceProviderFactory factory, string language)
            where TService : ILanguageService
        {
            return factory.GetLanguageServiceProvider(language).GetService<TService>();
        }
    }
}