// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides a description about a <see cref="Diagnostic"/>
    /// </summary>
    public sealed class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor>
    {
        /// <summary>
        /// An unique identifier for the diagnostic.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// A short localizable title describing the diagnostic.
        /// </summary>
        public LocalizableString Title { get; }

        /// <summary>
        /// An optional longer localizable description for the diagnostic.
        /// </summary>
        public LocalizableString Description { get; }

        /// <summary>
        /// An optional hyperlink that provides more detailed information regarding the diagnostic.
        /// </summary>
        public string HelpLinkUri { get; }

        /// <summary>
        /// A localizable format message string, which can be passed as the first argument to <see cref="String.Format(string, object[])"/> when creating the diagnostic message with this descriptor.
        /// </summary>
        /// <returns></returns>
        public LocalizableString MessageFormat { get; }

        /// <summary>
        /// The category of the diagnostic (like Design, Naming etc.)
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The default severity of the diagnostic.
        /// </summary>
        public DiagnosticSeverity DefaultSeverity { get; }

        /// <summary>
        /// Returns true if the diagnostic is enabled by default.
        /// </summary>
        public bool IsEnabledByDefault { get; }

        /// <summary>
        /// Custom tags for the diagnostic.
        /// </summary>
        public IEnumerable<string> CustomTags { get; }

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic"/>.
        /// NOTE: For localizable <paramref name="title"/>, <paramref name="description"/> and/or <paramref name="messageFormat"/>,
        /// use constructor overload <see cref="DiagnosticDescriptor(string, LocalizableString, LocalizableString, string, DiagnosticSeverity, bool, LocalizableString, string, string[])"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="title">A short title describing the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A format message string, which can be passed as the first argument to <see cref="String.Format(string, object[])"/> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'."</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        /// <param name="isEnabledByDefault">True if the diagnostic is enabled by default.</param>
        /// <param name="description">An optional longer description of the diagnostic.</param>
        /// <param name="helpLinkUri">An optional hyperlink that provides a more detailed description regarding the diagnostic.</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags"/> for some well known tags.</param>
        public DiagnosticDescriptor(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            string description = null,
            string helpLinkUri = null,
            params string[] customTags)
            : this(id, title, messageFormat, category, defaultSeverity, isEnabledByDefault, description, helpLinkUri, customTags.AsImmutableOrEmpty())
        {
        }

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="title">A short localizable title describing the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A localizable format message string, which can be passed as the first argument to <see cref="String.Format(string, object[])"/> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'."</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        /// <param name="isEnabledByDefault">True if the diagnostic is enabled by default.</param>
        /// <param name="description">An optional longer localizable description of the diagnostic.</param>
        /// <param name="helpLinkUri">An optional hyperlink that provides a more detailed description regarding the diagnostic.</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags"/> for some well known tags.</param>
        /// <remarks>Example descriptor for rule CA1001:
        ///     internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
        ///         new LocalizableResourceString(nameof(FxCopRulesResources.TypesThatOwnDisposableFieldsShouldBeDisposable), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources)),
        ///         new LocalizableResourceString(nameof(FxCopRulesResources.TypeOwnsDisposableFieldButIsNotDisposable), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources)),
        ///         FxCopDiagnosticCategory.Design,
        ///         DiagnosticSeverity.Warning,
        ///         isEnabledByDefault: true,
        ///         helpLinkUri: "http://msdn.microsoft.com/library/ms182172.aspx",
        ///         customTags: DiagnosticCustomTags.Microsoft);
        /// </remarks>
        public DiagnosticDescriptor(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString description = null,
            string helpLinkUri = null,
            params string[] customTags)
            : this(id, title, messageFormat, category, defaultSeverity, isEnabledByDefault, description, helpLinkUri, customTags.AsImmutableOrEmpty())
        {
        }

        internal DiagnosticDescriptor(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString description,
            string helpLinkUri,
            ImmutableArray<string> customTags)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(CodeAnalysisResources.DiagnosticIdCantBeNullOrWhitespace, nameof(id));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            this.Id = id;
            this.Title = title;
            this.Category = category;
            this.MessageFormat = messageFormat;
            this.DefaultSeverity = defaultSeverity;
            this.IsEnabledByDefault = isEnabledByDefault;
            this.Description = description ?? string.Empty;
            this.HelpLinkUri = helpLinkUri ?? string.Empty;
            this.CustomTags = customTags;
        }

        public bool Equals(DiagnosticDescriptor other)
        {
            return
                other != null &&
                this.Category == other.Category &&
                this.DefaultSeverity == other.DefaultSeverity &&
                this.Description.Equals(other.Description) &&
                this.HelpLinkUri == other.HelpLinkUri &&
                this.Id == other.Id &&
                this.IsEnabledByDefault == other.IsEnabledByDefault &&
                this.MessageFormat.Equals(other.MessageFormat) &&
                this.Title.Equals(other.Title);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DiagnosticDescriptor);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Category.GetHashCode(),
                Hash.Combine(this.DefaultSeverity.GetHashCode(),
                Hash.Combine(this.Description.GetHashCode(),
                Hash.Combine(this.HelpLinkUri.GetHashCode(),
                Hash.Combine(this.Id.GetHashCode(),
                Hash.Combine(this.IsEnabledByDefault.GetHashCode(),
                Hash.Combine(this.MessageFormat.GetHashCode(),
                    this.Title.GetHashCode())))))));
        }

        /// <summary>
        /// Gets the effective severity of diagnostics created based on this descriptor and the given <see cref="CompilationOptions"/>.
        /// </summary>
        /// <param name="compilationOptions">Compilation options</param>
        public ReportDiagnostic GetEffectiveSeverity(CompilationOptions compilationOptions)
        {
            if (compilationOptions == null)
            {
                throw new ArgumentNullException(nameof(compilationOptions));
            }

            // Create a dummy diagnostic to compute the effective diagnostic severity for given compilation options
            // TODO: Once https://github.com/dotnet/roslyn/issues/3650 is fixed, we can avoid creating a no-location diagnostic here.
            var effectiveDiagnostic = compilationOptions.FilterDiagnostic(Diagnostic.Create(this, Location.None));
            return effectiveDiagnostic != null ? MapSeverityToReport(effectiveDiagnostic.Severity) : ReportDiagnostic.Suppress;
        }

        // internal for testing purposes.
        internal static ReportDiagnostic MapSeverityToReport(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return ReportDiagnostic.Hidden;
                case DiagnosticSeverity.Info:
                    return ReportDiagnostic.Info;
                case DiagnosticSeverity.Warning:
                    return ReportDiagnostic.Warn;
                case DiagnosticSeverity.Error:
                    return ReportDiagnostic.Error;
                default:
                    throw ExceptionUtilities.UnexpectedValue(severity);
            }
        }

        /// <summary>
        /// Returns true if diagnostic descriptor is not configurable, i.e. cannot be suppressed or filtered or have its severity changed.
        /// For example, compiler errors are always non-configurable.
        /// </summary>
        internal bool IsNotConfigurable()
        {
            return AnalyzerManager.HasNotConfigurableTag(this.CustomTags);
        }
    }
}
