// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService
    {
        // this is temporary solution until option is supported in compiler layer natively
        // this won't serialize all options but some we pre-selected
        public void SerializeOptionSet(OptionSet options, string language, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteString(language);

            var serializationService = GetOptionsSerializationService(language);
            serializationService.WriteTo(options, writer, cancellationToken);
        }

        private OptionSet DeserializeOptionSet(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var language = reader.ReadString();

            var serializationService = GetOptionsSerializationService(language);
            return serializationService.ReadOptionSetFrom(reader, cancellationToken);
        }
    }
}
