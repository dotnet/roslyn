// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    /// <summary>
    /// Use this attribute to declare a <see cref="ISuppressionFixProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportSuppressionFixProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="ISuppressionFixProvider"/>.  
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The source languages this provider can provide fixes for.  See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; }

        public ExportSuppressionFixProviderAttribute(
            string name,
            params string[] languages)
            : base(typeof(ISuppressionFixProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            if (languages.Length == 0)
            {
                throw new ArgumentException("languages");
            }

            this.Name = name;
            this.Languages = languages;
        }
    }
}
