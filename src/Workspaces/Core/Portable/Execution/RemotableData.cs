// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Base for object that will use <see cref="IRemotableDataService"/> framework to synchronize data to remote host
    /// </summary>
    internal abstract partial class RemotableData
    {
        public static readonly RemotableData Null = new NullRemotableData();

        /// <summary>
        /// Indicates what kind of object it is
        /// <see cref="WellKnownSynchronizationKind"/> for examples.
        /// 
        /// this will be used in tranportation framework and deserialization service
        /// to hand shake how to send over data and deserialize serialized data
        /// </summary>
        public readonly WellKnownSynchronizationKind Kind;

        /// <summary>
        /// Checksum of this object
        /// </summary>
        public readonly Checksum Checksum;

        public RemotableData(Checksum checksum, WellKnownSynchronizationKind kind)
        {
            Checksum = checksum;
            Kind = kind;
        }

        /// <summary>
        /// This will write out this object's data (the data the checksum is associated with) to bits
        /// 
        /// this hide how each data is serialized to bits
        /// </summary>
        public abstract Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken);

        /// <summary>
        /// null asset indicating things that doesn't actually exist
        /// </summary>
        private sealed class NullRemotableData : RemotableData
        {
            public NullRemotableData()
                : base(Checksum.Null, WellKnownSynchronizationKind.Null)
            {
                // null object has null kind and null checksum. 
                // this null object is known to checksum framework and transportation framework to handle null case
                // properly.
            }

            public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            {
                // it write out nothing to stream. for null kind and checksum, checksum/transportation framework knows
                // there is no data in stream and skip reading any data from the stream.
                return Task.CompletedTask;
            }
        }
    }
}
