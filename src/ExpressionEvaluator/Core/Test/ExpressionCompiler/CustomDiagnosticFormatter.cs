// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class CustomDiagnosticFormatter : DiagnosticFormatter
    {
        internal new static readonly CustomDiagnosticFormatter Instance = new CustomDiagnosticFormatter();

        public override string Format(Diagnostic diagnostic, IFormatProvider formatter = null)
        {
            var cultureInfo = (CultureInfo)formatter;
            return string.Format("LCID={0}, Code={1}", cultureInfo.LCID, diagnostic.Code);
        }
    }
}
