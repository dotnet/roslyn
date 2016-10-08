// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Base for object that will use ISolutionSynchronizationService framework to synchronize data to remote host
    /// </summary>
    internal abstract partial class SynchronizationObject
    {
        public static readonly SynchronizationObject Null = new NullObject();

        /// <summary>
        /// Indicates what kind of checksum object it is
        /// <see cref="WellKnownSynchronizationKinds"/> for examples.
        /// 
        /// this later will be used to deserialize bits to actual object
        /// </summary>
        public readonly string Kind;

        /// <summary>
        /// Checksum of this object
        /// </summary>
        public readonly Checksum Checksum;

        public SynchronizationObject(Checksum checksum, string kind)
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
        private sealed class NullObject : SynchronizationObject
        {
            public NullObject() :
                base(Checksum.Null, WellKnownSynchronizationKinds.Null)
            {
            }

            public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }
        }
    }
}
