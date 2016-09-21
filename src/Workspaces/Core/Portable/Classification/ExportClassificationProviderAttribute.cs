// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Use this attribute to export a <see cref="ClassificationProvider"/> so that it will
    /// be found and used by a <see cref="ClassificationServiceWithProviders"/> service.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportClassificationProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }
        public string[] Roles { get; set; }

        public ExportClassificationProviderAttribute(string name, string language)
            : base(typeof(ClassificationProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            this.Name = name;
            this.Language = language;
        }
    }
}
