// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Roslyn.Core.Imaging
{
    //
    // Summary:
    //     Unique identifier for Visual Studio image asset.
    //
    // Remarks:
    //     On Windows systems, Microsoft.VisualStudio.Core.Imaging.ImageId can be converted
    //     to and from various other image representations via the ImageIdExtensions extension
    //     methods.
    [JsonConverter(typeof(ImageIdConverter))]
    internal struct ImageId : IEquatable<ImageId>
    {
        //
        // Summary:
        //     The Microsoft.VisualStudio.Core.Imaging.ImageId.Guid identifying the group to
        //     which this image belongs.
        public readonly Guid Guid;

        //
        // Summary:
        //     The System.Int32 identifying the particular image from the group that this id
        //     maps to.
        public readonly int Id;

        //
        // Summary:
        //     Creates a new instance of ImageId.
        //
        // Parameters:
        //   guid:
        //     The Microsoft.VisualStudio.Core.Imaging.ImageId.Guid identifying the group to
        //     which this image belongs.
        //
        //   id:
        //     The System.Int32 identifying the particular image from the group that this id
        //     maps to.
        public ImageId(Guid guid, int id)
        {
            Guid = guid;
            Id = id;
        }

        public override string ToString()
        {
            return ToString(CultureInfo.InvariantCulture);
        }

        public string ToString(IFormatProvider provider)
        {
            var guid = Guid;
            var arg = guid.ToString("D", provider);
            var id = Id;
            return string.Format(provider, "{0} : {1}", arg, id.ToString(provider));
        }

        bool IEquatable<ImageId>.Equals(ImageId other)
        {
            var id = Id;
            if (id.Equals(other.Id))
            {
                var guid = Guid;
                return guid.Equals(other.Guid);
            }

            return false;
        }

        public override bool Equals(object other)
        {
            if (other is ImageId)
            {
                var other2 = (ImageId)other;
                return ((IEquatable<ImageId>)this).Equals(other2);
            }

            return false;
        }

        public static bool operator ==(ImageId left, ImageId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageId left, ImageId right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            var guid = Guid;
            var hashCode = guid.GetHashCode();
            var id = Id;
            return hashCode ^ id.GetHashCode();
        }
    }
}
