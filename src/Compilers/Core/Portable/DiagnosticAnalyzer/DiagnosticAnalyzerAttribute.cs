// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a diagnostic analyzer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DiagnosticAnalyzerAttribute : Attribute
    {
        /// <summary>
        /// Analyzer attribute to be used for a diagnostic analyzer.
        /// If the analyzer is langauge agnostic, then no parameters need to be specified.
        /// Otherwise, if the analyzer is language specific, then specify a supported languages from <see cref="LanguageNames"/>.
        /// </summary>
        public DiagnosticAnalyzerAttribute()
        {
            this.SupportedLanguage = null;
        }

        /// <summary>
        /// Analyzer attribute to be used for a diagnostic analyzer.
        /// If the analyzer is langauge agnostic, then <paramref name="supportedLanguage"/> can be empty.
        /// Otherwise, if the analyzer is language specific, then specify a supported languages from <see cref="LanguageNames"/>.
        /// </summary>
        public DiagnosticAnalyzerAttribute(string supportedLanguage)
        {
            this.SupportedLanguage = supportedLanguage;
        }

        public string SupportedLanguage { get; private set; }
    }
}