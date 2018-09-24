// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal class DiagnosticSet
    {
        public string Description { get; private set; }
        public ImmutableArray<string> DiagnosticIds { get; private set; }

        public DiagnosticSet (string description, string[] diagnosticIds)
        {
            Description = description;
            DiagnosticIds = ImmutableArray.Create(diagnosticIds);
        }
    }
}
