// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class CSharpDiagnosticFormatter : DiagnosticFormatter
    {
        internal CSharpDiagnosticFormatter()
        {
        }

        internal override string GetWarnAsErrorMessage(CultureInfo culture)
        {
            return ErrorFacts.GetMessage(MessageID.IDS_WarnAsError, culture);
        }

        public new static readonly CSharpDiagnosticFormatter Instance = new CSharpDiagnosticFormatter();
    }
}