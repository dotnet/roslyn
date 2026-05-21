// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorCodeGenerationOptions
{
    private static int DefaultIndentSize => 4;
    private static string DefaultNewLine => Environment.NewLine;

    public static RazorCodeGenerationOptions Default { get; } = new(
        indentSize: DefaultIndentSize,
        newLine: DefaultNewLine,
        rootNamespace: null,
        cssScope: null,
        suppressUniqueIds: null,
        razorWarningLevel: 0,
        flags: Flags.DefaultFlags);

    public int IndentSize { get; }
    public string NewLine { get; }

    /// <summary>
    /// Gets the root namespace for the generated code.
    /// </summary>
    public string? RootNamespace { get; }

    /// <summary>
    /// A scope identifier that will be used on elements in the generated class, or <see langword="null""/>.
    /// </summary>
    public string? CssScope { get; }

    /// <summary>
    /// Gets a value used for unique ids for testing purposes. Null for unique ids.
    /// </summary>
    public string? SuppressUniqueIds { get; }

    /// <summary>
    /// Gets the warning level for diagnostic filtering. Diagnostics with a
    /// <see cref="RazorDiagnosticDescriptor.WarningLevel"/> greater than this value are suppressed.
    /// A value of <c>0</c> means only always-on diagnostics (level 0) are reported.
    /// </summary>
    public int RazorWarningLevel { get; }

    private readonly Flags _flags;

    private RazorCodeGenerationOptions(
        int indentSize,
        string newLine,
        string? rootNamespace,
        string? cssScope,
        string? suppressUniqueIds,
        int razorWarningLevel,
        Flags flags)
    {
        IndentSize = indentSize;
        NewLine = newLine;
        RootNamespace = rootNamespace;
        CssScope = cssScope;
        SuppressUniqueIds = suppressUniqueIds;
        RazorWarningLevel = razorWarningLevel;
        _flags = flags;
    }

    public static RazorCodeGenerationOptions Create(Action<Builder> configure)
    {
        var builder = new Builder();
        configure?.Invoke(builder);

        return builder.ToOptions();
    }

    public bool IndentWithTabs
        => (_flags & Flags.IndentWithTabs) == Flags.IndentWithTabs;

    /// <summary>
    /// Gets a value that indicates whether to suppress the default <c>#pragma checksum</c> directive in the
    /// generated C# code. If <c>false</c> the checksum directive will be included, otherwise it will not be
    /// generated. Defaults to <c>false</c>, meaning that the checksum will be included.
    /// </summary>
    /// <remarks>
    /// The <c>#pragma checksum</c> is required to enable debugging and should only be suppressed for testing
    /// purposes.
    /// </remarks>
    public bool SuppressChecksum
        => (_flags & Flags.SuppressChecksum) == Flags.SuppressChecksum;

    /// <summary>
    /// Gets a value that indicates whether to suppress the default metadata attributes in the generated
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
        => (_flags & Flags.SuppressMetadataAttributes) == Flags.SuppressMetadataAttributes;

    /// <summary>
    /// Gets a value that indicates whether to suppress the <c>RazorSourceChecksumAttribute</c>.
    /// <para>
    /// Used by default in .NET 6 apps since including a type-level attribute that changes on every
    /// edit are treated as rude edits by hot reload.
    /// </para>
    /// </summary>
    public bool SuppressMetadataSourceChecksumAttributes
        => (_flags & Flags.SuppressMetadataSourceChecksumAttributes) == Flags.SuppressMetadataSourceChecksumAttributes;

    /// <summary>
    /// Gets or sets a value that determines if an empty body is generated for the primary method.
    /// </summary>
    public bool SuppressPrimaryMethodBody
        => (_flags & Flags.SuppressPrimaryMethodBody) == Flags.SuppressPrimaryMethodBody;

    /// <summary>
    /// Gets a value that determines if nullability type enforcement should be suppressed for user code.
    /// </summary>
    public bool SuppressNullabilityEnforcement
        => (_flags & Flags.SuppressNullabilityEnforcement) == Flags.SuppressNullabilityEnforcement;

    /// <summary>
    /// Gets a value that determines if the components code writer may omit values for minimized attributes.
    /// </summary>
    public bool OmitMinimizedComponentAttributeValues
        => (_flags & Flags.OmitMinimizedComponentAttributeValues) == Flags.OmitMinimizedComponentAttributeValues;

    /// <summary>
    /// Gets a value that determines if localized component names are to be supported.
    /// </summary>
    public bool SupportLocalizedComponentNames
        => (_flags & Flags.SupportLocalizedComponentNames) == Flags.SupportLocalizedComponentNames;

    /// <summary>
    /// Gets a value that determines if enhanced line pragmas are to be utilized.
    /// </summary>
    public bool UseEnhancedLinePragma
        => (_flags & Flags.UseEnhancedLinePragma) == Flags.UseEnhancedLinePragma;

    /// <summary>
    /// Determines whether RenderTreeBuilder.AddComponentParameter should not be used.
    /// </summary>
    public bool SuppressAddComponentParameter
        => (_flags & Flags.SuppressAddComponentParameter) == Flags.SuppressAddComponentParameter;

    /// <summary>
    /// Determines if the file paths emitted as part of line pragmas should be mapped back to a valid path on windows.
    /// </summary>
    public bool RemapLinePragmaPathsOnWindows
        => (_flags & Flags.RemapLinePragmaPathsOnWindows) == Flags.RemapLinePragmaPathsOnWindows;

    /// <summary>
    /// Gets a value that determines if HTML literals should be written as C# UTF-8 string literals.
    /// </summary>
    public bool WriteHtmlUtf8StringLiterals
        => (_flags & Flags.WriteHtmlUtf8StringLiterals) == Flags.WriteHtmlUtf8StringLiterals;

    public RazorCodeGenerationOptions WithIndentSize(int value)
        => IndentSize == value
            ? this
            : new(value, NewLine, RootNamespace, CssScope, SuppressUniqueIds, RazorWarningLevel, _flags);

    public RazorCodeGenerationOptions WithNewLine(string value)
        => NewLine == value
            ? this
            : new(IndentSize, value, RootNamespace, CssScope, SuppressUniqueIds, RazorWarningLevel, _flags);

    public RazorCodeGenerationOptions WithRootNamespace(string? value)
        => RootNamespace == value
            ? this
            : new(IndentSize, NewLine, value, CssScope, SuppressUniqueIds, RazorWarningLevel, _flags);

    public RazorCodeGenerationOptions WithCssScope(string? value)
        => CssScope == value
            ? this
            : new(IndentSize, NewLine, RootNamespace, value, SuppressUniqueIds, RazorWarningLevel, _flags);

    public RazorCodeGenerationOptions WithSuppressUniqueIds(string? value)
        => SuppressUniqueIds == value
            ? this
            : new(IndentSize, NewLine, RootNamespace, CssScope, value, RazorWarningLevel, _flags);

    public RazorCodeGenerationOptions WithRazorWarningLevel(int value)
        => RazorWarningLevel == value
            ? this
            : new(IndentSize, NewLine, RootNamespace, CssScope, SuppressUniqueIds, value, _flags);

    public RazorCodeGenerationOptions WithFlags(
        bool? indentWithTabs = default,
        bool? suppressChecksum = default,
        bool? suppressMetadataAttributes = default,
        bool? suppressMetadataSourceChecksumAttributes = default,
        bool? suppressPrimaryMethodBody = default,
        bool? suppressNullabilityEnforcement = default,
        bool? omitMinimizedComponentAttributeValues = default,
        bool? supportLocalizedComponentNames = default,
        bool? useEnhancedLinePragma = default,
        bool? suppressAddComponentParameter = default,
        bool? remapLinePragmaPathsOnWindows = default,
        bool? writeHtmlUtf8StringLiterals = default)
    {
        var flags = _flags;

        if (indentWithTabs is bool indentWithTabsValue)
        {
            flags.UpdateFlag(Flags.IndentWithTabs, indentWithTabsValue);
        }

        if (suppressChecksum is bool suppressChecksumValue)
        {
            flags.UpdateFlag(Flags.SuppressChecksum, suppressChecksumValue);
        }

        if (suppressMetadataAttributes is bool suppressMetadataAttributesValue)
        {
            flags.UpdateFlag(Flags.SuppressMetadataAttributes, suppressMetadataAttributesValue);
        }

        if (suppressMetadataSourceChecksumAttributes is bool suppressMetadataSourceChecksumAttributesValue)
        {
            flags.UpdateFlag(Flags.SuppressMetadataSourceChecksumAttributes, suppressMetadataSourceChecksumAttributesValue);
        }

        if (suppressPrimaryMethodBody is bool suppressPrimaryMethodBodyValue)
        {
            flags.UpdateFlag(Flags.SuppressPrimaryMethodBody, suppressPrimaryMethodBodyValue);
        }

        if (suppressNullabilityEnforcement is bool suppressNullabilityEnforcementValue)
        {
            flags.UpdateFlag(Flags.SuppressNullabilityEnforcement, suppressNullabilityEnforcementValue);
        }

        if (omitMinimizedComponentAttributeValues is bool omitMinimizedComponentAttributeValuesValue)
        {
            flags.UpdateFlag(Flags.OmitMinimizedComponentAttributeValues, omitMinimizedComponentAttributeValuesValue);
        }

        if (supportLocalizedComponentNames is bool supportLocalizedComponentNamesValue)
        {
            flags.UpdateFlag(Flags.SupportLocalizedComponentNames, supportLocalizedComponentNamesValue);
        }

        if (useEnhancedLinePragma is bool useEnhancedLinePragmaValue)
        {
            flags.UpdateFlag(Flags.UseEnhancedLinePragma, useEnhancedLinePragmaValue);
        }

        if (suppressAddComponentParameter is bool suppressAddComponentParameterValue)
        {
            flags.UpdateFlag(Flags.SuppressAddComponentParameter, suppressAddComponentParameterValue);
        }

        if (remapLinePragmaPathsOnWindows is bool remapLinePragmaPathsOnWindowsValue)
        {
            flags.UpdateFlag(Flags.RemapLinePragmaPathsOnWindows, remapLinePragmaPathsOnWindowsValue);
        }

        if (writeHtmlUtf8StringLiterals is bool writeHtmlUtf8StringLiteralsValue)
        {
            flags.UpdateFlag(Flags.WriteHtmlUtf8StringLiterals, writeHtmlUtf8StringLiteralsValue);
        }

        return flags == _flags
            ? this
            : new(IndentSize, NewLine, RootNamespace, CssScope, SuppressUniqueIds, RazorWarningLevel, flags);
    }
}
