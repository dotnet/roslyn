// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
