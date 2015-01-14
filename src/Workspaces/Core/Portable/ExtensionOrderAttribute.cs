// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ExtensionOrderAttribute : Attribute
    {
        public ExtensionOrderAttribute()
        {
        }

        public string After { get; set; }

        public string Before { get; set; }
    }
}
