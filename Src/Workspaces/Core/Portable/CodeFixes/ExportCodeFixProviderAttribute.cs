// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Use this attribute to declare a <see cref="CodeFixProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportCodeFixProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="CodeFixProvider"/>.  
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The source languages this provider can provide fixes for.  See <see cref="LanguageNames"/>.
        /// </summary>
        public string[] Languages { get; private set; }

        public ExportCodeFixProviderAttribute(
            string name,
            params string[] languages)
            : base(typeof(CodeFixProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (languages == null)
            {
                throw new ArgumentNullException("languages");
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