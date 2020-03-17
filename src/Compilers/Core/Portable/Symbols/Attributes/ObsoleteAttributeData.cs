// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public static readonly ObsoleteAttributeData Uninitialized = new ObsoleteAttributeData(ObsoleteAttributeKind.Uninitialized, message: null, isError: false, diagnosticId: null, urlFormat: null);
        public static readonly ObsoleteAttributeData Experimental = new ObsoleteAttributeData(ObsoleteAttributeKind.Experimental, message: null, isError: false, diagnosticId: null, urlFormat: null);

        public ObsoleteAttributeData(ObsoleteAttributeKind kind, string? message, bool isError, string? diagnosticId, string? urlFormat)
        {
            Kind = kind;
            Message = message;
            IsError = isError;
            DiagnosticId = diagnosticId;
            UrlFormat = urlFormat;
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
        public readonly string? Message;

        public readonly string? DiagnosticId;

        public readonly string? UrlFormat;

        internal bool IsUninitialized
        {
            get { return ReferenceEquals(this, Uninitialized); }
        }
    }
}
