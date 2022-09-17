// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Context and preferences.
    /// </summary>
    internal abstract class CodeGenerationContextInfo
    {
        public readonly CodeGenerationContext Context;

        protected CodeGenerationContextInfo(CodeGenerationContext context)
        {
            Context = context;
        }

        public CodeGenerationContextInfo WithContext(CodeGenerationContext value)
            => WithContextImpl(value);

        public CodeGenerationOptions Options => OptionsImpl;

        protected abstract CodeGenerationOptions OptionsImpl { get; }
        protected abstract CodeGenerationContextInfo WithContextImpl(CodeGenerationContext value);
    }
}
