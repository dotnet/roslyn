// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [MetadataAttribute]
    internal sealed class QuickInfoConverterMetadataAttribute : Attribute
    {
        public QuickInfoConverterMetadataAttribute(Type deferredType)
        {
            DeferredTypeFullName = deferredType.FullName;
        }

        public string DeferredTypeFullName { get; }
    }
}
