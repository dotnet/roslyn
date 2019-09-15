// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IConfigurationFixProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ExportConfigurationFixProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="IConfigurationFixProvider"/>.  
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The source languages this provider can provide fixes for.  See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        public ExportConfigurationFixProviderAttribute(
            string name,
            params string[] languages)
            : base(typeof(IConfigurationFixProvider))
        {
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            if (languages.Length == 0)
            {
                throw new ArgumentException(nameof(languages));
            }

            Languages = languages;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
