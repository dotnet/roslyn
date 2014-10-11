// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    public sealed class SerializableProjectReference : ISerializable
    {
        private readonly ProjectReference projectReference;

        public SerializableProjectReference(ProjectReference projectReference)
        {
            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            this.projectReference = projectReference;
        }

        private SerializableProjectReference(SerializationInfo info, StreamingContext context)
        {
            var projectId = ((SerializableProjectId)info.GetValue("projectId", typeof(SerializableProjectId))).ProjectId;
            var aliases = ImmutableArray.Create((string[])info.GetValue("aliases", typeof(string[])));
            var embedInteropTypes = info.GetBoolean("embedInteropTypes");

            this.projectReference = new ProjectReference(projectId, aliases, embedInteropTypes);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("projectId", new SerializableProjectId(this.projectReference.ProjectId));
            info.AddValue("aliases", this.projectReference.Aliases.IsDefault ? null : this.projectReference.Aliases.ToArray(), typeof(string[]));
            info.AddValue("embedInteropTypes", this.projectReference.EmbedInteropTypes);
        }

        public ProjectReference ProjectReference
        {
            get { return projectReference; }
        }
    }
}
