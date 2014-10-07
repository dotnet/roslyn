// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExportDiagnosticAnalyzerAttribute : ExportAttribute
    {
        public string[] Languages { get; private set; }

        public ExportDiagnosticAnalyzerAttribute(
            params string[] languages)
            : base(typeof(IDiagnosticAnalyzer))
        {
            if (languages == null)
            {
                throw new ArgumentNullException("languages");
            }

            if (languages.Length == 0)
            {
                throw new ArgumentException("languages");
            }

            this.Languages = languages;
        }
    }
}