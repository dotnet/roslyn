// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportOptionProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// Optional source language for language specific option providers.  See <see cref="LanguageNames"/>.
        /// This will be empty string for language agnostic option providers.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// Constructor for language agnostic option providers.
        /// Use <see cref="ExportOptionProviderAttribute(string)"/> overload for language specific option providers.
        /// </summary>
        public ExportOptionProviderAttribute()
            : base(typeof(IOptionProvider))
        {
            this.Language = string.Empty;
        }

        /// <summary>
        /// Constructor for language specific option providers.
        /// Use <see cref="ExportOptionProviderAttribute()"/> overload for language agnostic option providers.
        /// </summary>
        public ExportOptionProviderAttribute(string language)
            : base(typeof(IOptionProvider))
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
