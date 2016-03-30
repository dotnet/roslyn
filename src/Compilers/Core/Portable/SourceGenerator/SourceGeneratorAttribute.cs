// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SourceGeneratorAttribute : Attribute
    {
        public string[] Languages { get; }

        public SourceGeneratorAttribute(string firstLanguage, params string[] additionalLanguages)
        {
            if (firstLanguage == null)
            {
                throw new ArgumentNullException(nameof(firstLanguage));
            }
            if (additionalLanguages == null)
            {
                throw new ArgumentNullException(nameof(additionalLanguages));
            }
            var builder = ArrayBuilder<string>.GetInstance();
            builder.Add(firstLanguage);
            builder.AddRange(additionalLanguages);
            Languages = builder.ToArrayAndFree();
        }
    }
}
