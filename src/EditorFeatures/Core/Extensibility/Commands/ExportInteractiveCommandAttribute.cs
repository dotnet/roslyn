// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
