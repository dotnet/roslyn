// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal abstract class ExportOptionProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// Optional source language for language specific option providers.  See <see cref="LanguageNames"/>.
        /// This will be empty string for language agnostic option providers.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// True if the option is a client global option provided by <see cref="IGlobalOptionService"/>.
        /// </summary>
        public bool IsGlobal { get; }

        public ExportOptionProviderAttribute(string language, bool isGlobal)
            : base(typeof(IOptionProvider))
        {
            Language = language;
            IsGlobal = isGlobal;
        }
    }

    /// <summary>
    /// Global client-only options.
    /// </summary>
    internal sealed class ExportGlobalOptionProviderAttribute : ExportOptionProviderAttribute
    {
        public ExportGlobalOptionProviderAttribute()
            : this(language: string.Empty)
        {
        }

        public ExportGlobalOptionProviderAttribute(string language)
            : base(language, isGlobal: true)
        {
        }
    }

    /// <summary>
    /// Options that are part of the solution snapshot.
    /// Some of these options may be configurable per document via editorconfig.
    /// </summary>
    internal sealed class ExportSolutionOptionProviderAttribute : ExportOptionProviderAttribute
    {
        public ExportSolutionOptionProviderAttribute()
            : this(language: string.Empty)
        {
        }

        public ExportSolutionOptionProviderAttribute(string language)
            : base(language, isGlobal: false)
        {
        }
    }
}
