// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal enum ObsoleteAttributeKind
    {
        None,
        Uninitialized,
        Obsolete,
        Deprecated,
        Experimental,
    }

    /// <summary>
    /// Information decoded from <see cref="ObsoleteAttribute"/>.
    /// </summary>
    internal sealed class ObsoleteAttributeData
    {
        public static readonly ObsoleteAttributeData Uninitialized = new ObsoleteAttributeData(ObsoleteAttributeKind.Uninitialized, null, false);
        public static readonly ObsoleteAttributeData Experimental = new ObsoleteAttributeData(ObsoleteAttributeKind.Experimental, null, false);

        public ObsoleteAttributeData(ObsoleteAttributeKind kind, string message, bool isError)
        {
            Kind = kind;
            Message = message;
            IsError = isError;
        }

        public readonly ObsoleteAttributeKind Kind;

        /// <summary>
        /// True if an error should be thrown for the <see cref="ObsoleteAttribute"/>. Default is false in which case
        /// a warning is thrown.
        /// </summary>
        public readonly bool IsError;

        /// <summary>
        /// The message that will be shown when an error/warning is created for <see cref="ObsoleteAttribute"/>.
        /// </summary>
        public readonly string Message;

        internal bool IsUninitialized
        {
            get { return ReferenceEquals(this, Uninitialized); }
        }
    }
}
