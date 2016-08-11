// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportQuickInfoPresentationProviderAttribute : ExportAttribute
    {
        public string[] Kinds { get; }

        public ExportQuickInfoPresentationProviderAttribute(params string[] kinds)
            : base(typeof(QuickInfoPresentationProvider))
        {
            this.Kinds = kinds;
        }
    }
}
