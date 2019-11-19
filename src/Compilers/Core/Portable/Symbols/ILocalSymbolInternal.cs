// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ILocalSymbolInternal : ISymbolInternal
    {
        bool IsImportedFromMetadata { get; }

        SynthesizedLocalKind SynthesizedKind { get; }

        SyntaxNode GetDeclaratorSyntax();
    }
}
