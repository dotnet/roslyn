// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    public sealed class SerializableProjectReference : ISerializable
    {
        private readonly ProjectReference _projectReference;

        public SerializableProjectReference(ProjectReference projectReference)
        {
            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            _projectReference = projectReference;
        }

        private SerializableProjectReference(SerializationInfo info, StreamingContext context)
        {
            var projectId = ((SerializableProjectId)info.GetValue("projectId", typeof(SerializableProjectId))).ProjectId;
            var aliases = ImmutableArray.Create((string[])info.GetValue("aliases", typeof(string[])));
            var embedInteropTypes = info.GetBoolean("embedInteropTypes");

            _projectReference = new ProjectReference(projectId, aliases, embedInteropTypes);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("projectId", new SerializableProjectId(_projectReference.ProjectId));
            info.AddValue("aliases", _projectReference.Aliases.IsEmpty ? null : _projectReference.Aliases.ToArray(), typeof(string[]));
            info.AddValue("embedInteropTypes", _projectReference.EmbedInteropTypes);
        }

        public ProjectReference ProjectReference
        {
            get { return _projectReference; }
        }
    }
}
