// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    /// <summary>
    /// A base class that can be used to implement a Source Generator 
    /// </summary>
    public abstract class SourceGenerator : ISourceGenerator
    {
        public abstract void Execute(SourceGeneratorContext context);

        public virtual void Initialize(InitializationContext context)
        {
        }
    }
}
