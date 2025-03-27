// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class XmlSyntaxDiagnosticInfo : SyntaxDiagnosticInfo
    {
        private readonly XmlParseErrorCode _xmlErrorCode;

        internal XmlSyntaxDiagnosticInfo(XmlParseErrorCode code, params object[] args)
            : this(0, 0, code, args)
        {
        }

        internal XmlSyntaxDiagnosticInfo(int offset, int width, XmlParseErrorCode code, params object[] args)
            : base(offset, width, ErrorCode.WRN_XMLParseError, args)
        {
            _xmlErrorCode = code;
        }

        private XmlSyntaxDiagnosticInfo(XmlSyntaxDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
        {
            _xmlErrorCode = original._xmlErrorCode;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new XmlSyntaxDiagnosticInfo(this, severity);
        }

        public override string GetMessage(IFormatProvider? formatProvider = null)
        {
            var culture = formatProvider as CultureInfo;

            string messagePrefix = this.MessageProvider.LoadMessage(this.Code, culture);
            string message = ErrorFacts.GetMessage(_xmlErrorCode, culture);

            RoslynDebug.Assert(!string.IsNullOrEmpty(message));

            if (this.Arguments == null || this.Arguments.Length == 0)
            {
                return String.Format(formatProvider, messagePrefix, message);
            }

            return String.Format(formatProvider, String.Format(formatProvider, messagePrefix, message), GetArgumentsToUse(formatProvider));
        }
    }
}
