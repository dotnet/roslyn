// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
