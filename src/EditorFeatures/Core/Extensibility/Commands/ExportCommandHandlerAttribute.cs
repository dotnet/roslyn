// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ExportCommandHandlerAttribute : ExportAttribute
    {
        public string Name { get; }
        public IEnumerable<string> ContentTypes { get; }

        public ExportCommandHandlerAttribute(string name, params string[] contentTypes)
            : base(typeof(ICommandHandler))
        {
            this.Name = name;
            this.ContentTypes = contentTypes;
        }
    }
}
