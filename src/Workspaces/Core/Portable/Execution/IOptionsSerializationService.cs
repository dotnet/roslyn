﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
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
