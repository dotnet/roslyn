// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportInteractiveAttribute : ExportAttribute
    {
        public IEnumerable<string> ContentTypes { get; }

        public ExportInteractiveAttribute(Type t, params string[] contentTypes)
            : base(t)
        {
            this.ContentTypes = contentTypes;
        }
    }
}
