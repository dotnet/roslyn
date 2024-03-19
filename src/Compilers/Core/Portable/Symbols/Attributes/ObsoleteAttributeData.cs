// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal enum ObsoleteAttributeKind
    {
        None,
        Uninitialized,
        Obsolete,
        Deprecated,
        WindowsExperimental,
        Experimental,
    }

    /// <summary>
    /// Information decoded from <see cref="ObsoleteAttribute"/>.
    /// </summary>
    internal sealed class ObsoleteAttributeData
    {
        public static readonly ObsoleteAttributeData Uninitialized = new ObsoleteAttributeData(ObsoleteAttributeKind.Uninitialized, message: null, isError: false, diagnosticId: null, urlFormat: null);
        public static readonly ObsoleteAttributeData WindowsExperimental = new ObsoleteAttributeData(ObsoleteAttributeKind.WindowsExperimental, message: null, isError: false, diagnosticId: null, urlFormat: null);

        public const string DiagnosticIdPropertyName = "DiagnosticId";
        public const string UrlFormatPropertyName = "UrlFormat";

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

        /// <summary>
        /// The custom diagnostic ID to use for obsolete diagnostics.
        /// If null, diagnostics are produced using the compiler default diagnostic IDs.
        /// </summary>
        public readonly string? DiagnosticId;

        /// <summary>
        /// <para>
        /// The custom help URL format string for obsolete diagnostics.
        /// Expected to contain zero or one format items.
        /// </para>
        /// <para>
        /// When specified, the obsolete diagnostic's <see cref="DiagnosticDescriptor.HelpLinkUri"/> will be produced
        /// by formatting this string using the <see cref="DiagnosticId"/> as the single argument.
        /// </para>
        /// 
        /// <example>
        /// e.g. with a <see cref="DiagnosticId"/> value <c>"TEST1"</c>,
        /// and a <see cref="UrlFormat"/> value <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/{0}/"/>,<br/>
        /// the diagnostic will have the HelpLinkUri <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/TEST1/"/>.
        /// </example>
        /// </summary>
        public readonly string? UrlFormat;

        internal bool IsUninitialized
        {
            get { return ReferenceEquals(this, Uninitialized); }
        }
    }
}
