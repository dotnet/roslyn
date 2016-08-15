// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SymbolCategorization
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportSymbolCategorizerAttribute : ExportAttribute
    {
        public ExportSymbolCategorizerAttribute() : base(typeof(ISymbolCategorizer))
        {
        }
    }
}