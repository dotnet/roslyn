// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Options.Providers
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class ExportOptionProviderAttribute : ExportAttribute
    {
        public ExportOptionProviderAttribute() : base(typeof(IOptionProvider))
        {
        }
    }
}
