// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a diagnostic analyzer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DiagnosticAnalyzerAttribute : Attribute
    {
        private readonly ImmutableArray<string> supportedLanguages;

        /// <summary>
        /// Analyzer attribute to be used for diagnostic analyzer.
        /// If the analyzer is langauge agnostic, then <paramref name="supportedLanguages"/> can be empty.
        /// Otherwise, if the analyzer is language specific, then specify the set of supported languages from <see cref="LanguageNames"/>.
        /// </summary>
        public DiagnosticAnalyzerAttribute(params string[] supportedLanguages)
        {
            this.supportedLanguages = supportedLanguages.AsImmutableOrEmpty();
        }

        internal bool IsSupported(string language)
        {
            return this.supportedLanguages.IsEmpty || this.supportedLanguages.Contains(language);
        }
    }
}