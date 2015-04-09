// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Options
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ExportOptionAttribute : ExportAttribute
    {
        public ExportOptionAttribute()
            : base(typeof(IOption))
        {
        }
    }
}