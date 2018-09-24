// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICompilationOptionsService), LanguageNames.CSharp), Shared]
    internal class CSharpCompilationOptionsService : ICompilationOptionsService
    {
        public bool SupportsUnsafe => true;

        public bool GetAllowUnsafe(CompilationOptions options)
            => ((CSharpCompilationOptions)options).AllowUnsafe;

        public CompilationOptions WithAllowUnsafe(CompilationOptions old, bool allowUnsafe)
            => ((CSharpCompilationOptions)old).WithAllowUnsafe(allowUnsafe);
    }
}
