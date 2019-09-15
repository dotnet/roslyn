// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Use this attribute to export a <see cref="QuickInfoProvider"/> so that it will
    /// be found and used by the per language associated <see cref="QuickInfoService"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
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
