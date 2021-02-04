// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Completion
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportArgumentProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }
        public string[] Roles { get; set; }

        public ExportArgumentProviderAttribute(string name, string language)
            : base(typeof(ArgumentProvider))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Roles = Array.Empty<string>();
        }
    }
}
