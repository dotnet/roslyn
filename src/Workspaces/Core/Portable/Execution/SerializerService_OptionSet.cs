// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService
    {
        public void SerializeOptionSet(SerializableOptionSet options, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            options.Serialize(writer, cancellationToken);
        }

        private SerializableOptionSet DeserializeOptionSet(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var optionService = _workspaceServices.GetRequiredService<IOptionService>();
            return SerializableOptionSet.Deserialize(reader, optionService, cancellationToken);
        }
    }
}
