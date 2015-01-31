﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class XmlSyntaxDiagnosticInfo : SyntaxDiagnosticInfo
    {
        private readonly XmlParseErrorCode xmlErrorCode;
    internal XmlSyntaxDiagnosticInfo(XmlParseErrorCode code, string arg)
         : this(0, 0, code, arg)
    {
    }
    internal XmlSyntaxDiagnosticInfo(XmlParseErrorCode code, params object[] args)
            : this(0, 0, code, args)
        {
        }

        internal XmlSyntaxDiagnosticInfo(int offset, int width, XmlParseErrorCode code, params object[] args)
            : base(offset, width, ErrorCode.WRN_XMLParseError, args)
        {
            this.xmlErrorCode = code;
        }

        #region Serialization

        protected override void WriteTo(ObjectWriter writer)
        {
            base.WriteTo(writer);
            writer.WriteCompressedUInt((uint)this.xmlErrorCode);
        }

        protected override Func<ObjectReader, object> GetReader()
        {
            return r => new XmlSyntaxDiagnosticInfo(r);
        }

        private XmlSyntaxDiagnosticInfo(ObjectReader reader)
            : base(reader)
        {
            this.xmlErrorCode = (XmlParseErrorCode)reader.ReadCompressedUInt();
        }

        #endregion

        public override string GetMessage(IFormatProvider formatProvider = null)
        {
            var culture = formatProvider as CultureInfo;

            string messagePrefix = this.MessageProvider.LoadMessage(this.Code, culture);
            string message = ErrorFacts.GetMessage(xmlErrorCode, culture);

            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(message));

            if (this.Arguments == null || this.Arguments.Length == 0)
            {
                return String.Format(formatProvider, messagePrefix, message);
            }

            return String.Format(formatProvider, String.Format(formatProvider, messagePrefix, message), GetArgumentsToUse(formatProvider));
        }
    }
}
