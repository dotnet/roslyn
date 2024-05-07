// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Response class when asking server to resolve the rendering information of a string kind.
    /// </summary>
    [DataContract]
    internal class VSInternalIconMapping : IEquatable<VSInternalIconMapping>
    {
        /// <summary>
        /// Gets or sets the ImageElements for a certain kind.
        /// </summary>
        [DataMember(Name = "_vs_images")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSImageId[]? Images
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tags for a certain kind. To be used in the absence of ImageIds.
        /// </summary>
        [DataMember(Name = "_vs_tags")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? Tags
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return this.Equals(obj as VSInternalIconMapping);
        }

        /// <inheritdoc/>
        public bool Equals(VSInternalIconMapping? other)
        {
            if (other == null)
            {
                return false;
            }

            return CheckImagesAreEqual(this.Images, other.Images) && CheckTagsAreEqual(this.Tags, other.Tags);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1825600323;

            if (this.Images != null)
            {
                for (var i = 0; i < this.Images.Length; i++)
                {
                    hashCode = (hashCode * -1521134295) + this.Images[i].Guid.GetHashCode();
                    hashCode = (hashCode * -1521134295) + this.Images[i].Id.GetHashCode();
                }
            }

            if (this.Tags != null)
            {
                for (var i = 0; i < this.Tags.Length; i++)
                {
                    hashCode = (hashCode * -1521134295) + StringComparer.Ordinal.GetHashCode(this.Tags[i]);
                }
            }

            return hashCode;
        }

        private static bool CheckImagesAreEqual(VSImageId[]? current, VSImageId[]? other)
        {
            if (current == null ^ other == null)
            {
                return false;
            }

            if (current != null &&
                other != null &&
                current.Length == other.Length)
            {
                for (var i = 0; i < current.Length; i++)
                {
                    if (current[i].Id != other[i].Id)
                    {
                        return false;
                    }

                    if (current[i].Guid != other[i].Guid)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CheckTagsAreEqual(string[]? current, string[]? other)
        {
            if (current == null ^ other == null)
            {
                return false;
            }

            if (current != null &&
                other != null &&
                current.Length == other.Length)
            {
                for (var i = 0; i < current.Length; i++)
                {
                    if (!string.Equals(current[i], other[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
