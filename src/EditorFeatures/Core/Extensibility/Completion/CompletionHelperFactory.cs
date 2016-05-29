// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class CompletionHelperFactory : ILanguageService
    {
        public abstract CompletionHelper CreateCompletionHelper(CompletionService completionService);
    }
}