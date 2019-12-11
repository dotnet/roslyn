// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An implementation of <see cref="OptionSet"/> that is serializable.
    /// </summary>
    internal abstract class SerializableOptionSet : OptionSet
    {
        public abstract void Serialize(ObjectWriter writer, CancellationToken cancellationToken);

        public abstract void Deserialize(ObjectReader reader, CancellationToken cancellationToken);
    }
}
