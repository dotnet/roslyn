// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This deal with serializing/deserializing language specific data
    /// </summary>
    internal interface IOptionsSerializationService : ILanguageService
    {
        void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken);
        void WriteTo(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken);

        CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
        ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
    }
}
