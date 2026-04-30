// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorCodeGenerationOptions
{
    public sealed class Builder
    {
        private Flags _flags;

        public int IndentSize { get; set; }
        public string NewLine { get; set => field = value ?? DefaultNewLine; }

        /// <summary>
        /// Gets or sets the root namespace of the generated code.
        /// </summary>
        public string? RootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the scope identifier to be used on elements in the generated class.
        /// </summary>
        public string? CssScope { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if unique ids are suppressed for testing.
        /// </summary>
        public string? SuppressUniqueIds { get; set; }

        /// <summary>
        /// Gets or sets the warning level for diagnostic filtering.
        /// </summary>
        public int RazorWarningLevel { get; set; }

        internal Builder()
        {
            IndentSize = DefaultIndentSize;
            NewLine = DefaultNewLine;
        }

        public bool DesignTime
        {
            get => _flags.IsFlagSet(Flags.DesignTime);
            set => _flags.UpdateFlag(Flags.DesignTime, value);
        }

        public bool IndentWithTabs
        {
            get => _flags.IsFlagSet(Flags.IndentWithTabs);
            set => _flags.UpdateFlag(Flags.IndentWithTabs, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to suppress the default <c>#pragma checksum</c> directive in the
        /// generated C# code. If <c>false</c> the checksum directive will be included, otherwise it will not be
        /// generated. Defaults to <c>false</c>, meaning that the checksum will be included.
        /// </summary>
        /// <remarks>
        /// The <c>#pragma checksum</c> is required to enable debugging and should only be suppressed for testing
        /// purposes.
        /// </remarks>
        public bool SuppressChecksum
        {
            get => _flags.IsFlagSet(Flags.SuppressChecksum);
            set => _flags.UpdateFlag(Flags.SuppressChecksum, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to suppress the default metadata attributes in the generated
        /// C# code. If <c>false</c> the default attributes will be included, otherwise they will not be generated.
        /// Defaults to <c>false</c> at run time, meaning that the attributes will be included. Defaults to
        /// <c>true</c> at design time, meaning that the attributes will not be included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <c>Microsoft.AspNetCore.Razor.Runtime</c> package includes a default set of attributes intended
        /// for runtimes to discover metadata about the compiled code.
        /// </para>
        /// <para>
        /// The default metadata attributes should be suppressed if code generation targets a runtime without
        /// a reference to <c>Microsoft.AspNetCore.Razor.Runtime</c>, or for testing purposes.
        /// </para>
        /// </remarks>
        public bool SuppressMetadataAttributes
        {
            get => _flags.IsFlagSet(Flags.SuppressMetadataAttributes);
            set => _flags.UpdateFlag(Flags.SuppressMetadataAttributes, value);
        }

        /// <summary>
        /// Gets a value that indicates whether to suppress the <c>RazorSourceChecksumAttribute</c>.
        /// <para>
        /// Used by default in .NET 6 apps since including a type-level attribute that changes on every
        /// edit are treated as rude edits by hot reload.
        /// </para>
        /// </summary>
        public bool SuppressMetadataSourceChecksumAttributes
        {
            get => _flags.IsFlagSet(Flags.SuppressMetadataSourceChecksumAttributes);
            set => _flags.UpdateFlag(Flags.SuppressMetadataSourceChecksumAttributes, value);
        }

        /// <summary>
        /// Gets or sets a value that determines if an empty body is generated for the primary method.
        /// </summary>
        public bool SuppressPrimaryMethodBody
        {
            get => _flags.IsFlagSet(Flags.SuppressPrimaryMethodBody);
            set => _flags.UpdateFlag(Flags.SuppressPrimaryMethodBody, value);
        }

        /// <summary>
        /// Gets or sets a value that determines if nullability type enforcement should be suppressed for user code.
        /// </summary>
        public bool SuppressNullabilityEnforcement
        {
            get => _flags.IsFlagSet(Flags.SuppressNullabilityEnforcement);
            set => _flags.UpdateFlag(Flags.SuppressNullabilityEnforcement, value);
        }

        /// <summary>
        /// Gets or sets a value that determines if the components code writer may omit values for minimized attributes.
        /// </summary>
        public bool OmitMinimizedComponentAttributeValues
        {
            get => _flags.IsFlagSet(Flags.OmitMinimizedComponentAttributeValues);
            set => _flags.UpdateFlag(Flags.OmitMinimizedComponentAttributeValues, value);
        }

        /// <summary>
        /// Gets or sets a value that determines if localized component names are to be supported.
        /// </summary>
        public bool SupportLocalizedComponentNames
        {
            get => _flags.IsFlagSet(Flags.SupportLocalizedComponentNames);
            set => _flags.UpdateFlag(Flags.SupportLocalizedComponentNames, value);
        }

        /// <summary>
        /// Gets or sets a value that determines if enhanced line pragmas are to be utilized.
        /// </summary>
        public bool UseEnhancedLinePragma
        {
            get => _flags.IsFlagSet(Flags.UseEnhancedLinePragma);
            set => _flags.UpdateFlag(Flags.UseEnhancedLinePragma, value);
        }

        /// <summary>
        /// Determines whether RenderTreeBuilder.AddComponentParameter should not be used.
        /// </summary>
        public bool SuppressAddComponentParameter
        {
            get => _flags.IsFlagSet(Flags.SuppressAddComponentParameter);
            set => _flags.UpdateFlag(Flags.SuppressAddComponentParameter, value);
        }

        /// <summary>
        /// Determines if the file paths emitted as part of line pragmas should be mapped back to a valid path on windows.
        /// </summary>
        public bool RemapLinePragmaPathsOnWindows
        {
            get => _flags.IsFlagSet(Flags.RemapLinePragmaPathsOnWindows);
            set => _flags.UpdateFlag(Flags.RemapLinePragmaPathsOnWindows, value);
        }

        public RazorCodeGenerationOptions ToOptions()
            => new(IndentSize, NewLine, RootNamespace, CssScope, SuppressUniqueIds, RazorWarningLevel, _flags);
    }
}
