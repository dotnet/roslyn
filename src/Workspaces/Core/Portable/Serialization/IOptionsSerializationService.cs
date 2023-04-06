// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ExportOptionsSerializationService : ExportAttribute, ILanguageMetadata
    {
        public string Language { get; }

        public ExportOptionsSerializationService(string language) : base(typeof(IOptionsSerializationService))
        {
            Language = language;
        }
    }

    /// <summary>
    /// This deal with serializing/deserializing language specific data
    /// </summary>
    internal interface IOptionsSerializationService
    {
        void WriteTo(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken);
        void WriteTo(ParseOptions options, ObjectWriter writer);

        CompilationOptions ReadCompilationOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
        ParseOptions ReadParseOptionsFrom(ObjectReader reader, CancellationToken cancellationToken);
    }
}
