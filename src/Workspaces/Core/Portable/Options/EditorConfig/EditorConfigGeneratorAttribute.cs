// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Options
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class EditorConfigGeneratorAttribute : ExportAttribute
    {
        public string Language { get; }

        public EditorConfigGeneratorAttribute(string language)
            : base(typeof(IEditorConfigOptionsCollection))
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
