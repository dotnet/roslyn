// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents no location at all. Useful for errors in command line options, for example.
    /// </summary>
    /// <remarks></remarks>
    [Serializable]
    internal sealed class NoLocation : Location, IObjectReference
    {
        public static readonly Location Singleton = new NoLocation();

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return Singleton;
        }

        private NoLocation()
        {
        }

        public override LocationKind Kind
        {
            get { return LocationKind.None; }
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            // arbitrary number, since all NoLocation's are equal
            return 0x16487756;
        }
    }
}