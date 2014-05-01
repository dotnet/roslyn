// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    internal class CSharpCodeCleanerService : AbstractCodeCleanerService
    {
        private static readonly IEnumerable<ICodeCleanupProvider> defaultProviders;

        static CSharpCodeCleanerService()
        {
            // TODO : move it down to service and add this - GetService<IOrganizingService>(LanguageNames.VisualBasic)

            defaultProviders = ImmutableArray.Create<ICodeCleanupProvider>(
                new SimplificationCodeCleanupProvider(),
                new FormatCodeCleanupProvider());
        }

        public override IEnumerable<ICodeCleanupProvider> GetDefaultProviders()
        {
            return defaultProviders;
        }
    }
}