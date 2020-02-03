// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Use this attribute to export a <see cref="QuickInfoProvider"/> so that it will
    /// be found and used by the per language associated <see cref="QuickInfoService"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportQuickInfoProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportQuickInfoProviderAttribute(string name, string language)
            : base(typeof(QuickInfoProvider))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
