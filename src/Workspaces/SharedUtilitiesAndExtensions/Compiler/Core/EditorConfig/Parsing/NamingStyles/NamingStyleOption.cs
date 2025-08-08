// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;

/// <summary>
/// The root naming style option composed of several settings as well as a <see cref="TextSpan"/>s describing where they were all defined. 
/// </summary>
/// <param name="Section">The section of the editorconfig file this option applies to.</param>
/// <param name="RuleName">The name given to thie option in the file.</param>
/// <param name="ApplicableSymbolInfo">The kinds of symbols this option applies to.</param>
/// <param name="NamingScheme">The rules about how the specified symbols must be named.</param>
/// <param name="Severity">The keve of build error that should be produced when a matching symbol does not meetthe naming requirements.</param>
internal sealed record class NamingStyleOption(
    Section Section,
    EditorConfigOption<string> RuleName,
    ApplicableSymbolInfo ApplicableSymbolInfo,
    NamingScheme NamingScheme,
    EditorConfigOption<ReportDiagnostic> Severity)
    : EditorConfigOption(Section, RuleName.Span);

/// <summary>
/// A description of the kinds of symbols a rule should apply to as well as a <see cref="TextSpan"/>s describing where they were all defined. 
/// </summary>
/// <param name="OptionName">The name given to thie option in the file.</param>
/// <param name="SymbolKinds">The kinds of symbols this option applies to.</param>
/// <param name="Accessibilities">The accessibilities of symbols this option applies to.</param>
/// <param name="Modifiers">The required modifier that must be present on symbols this option applies to.</param>
internal sealed record class ApplicableSymbolInfo(
    EditorConfigOption<string> OptionName,
    EditorConfigOption<ImmutableArray<SymbolKindOrTypeKind>> SymbolKinds,
    EditorConfigOption<ImmutableArray<Accessibility>> Accessibilities,
    EditorConfigOption<ImmutableArray<ModifierKind>> Modifiers);

/// <summary>
/// The rules about how the specified symbols must be named as well as a <see cref="TextSpan"/>s describing where they were all defined. 
/// </summary>
/// <param name="OptionName">The name given to thie option in the file.</param>
/// <param name="Prefix">Required suffix</param>
/// <param name="Suffix">Required prefix</param>
/// <param name="WordSeparator">Required word separator characters</param>
/// <param name="Capitalization">The capitalization scheme</param>
internal sealed record class NamingScheme(
    EditorConfigOption<string> OptionName,
    EditorConfigOption<string?> Prefix,
    EditorConfigOption<string?> Suffix,
    EditorConfigOption<string?> WordSeparator,
    EditorConfigOption<Capitalization> Capitalization);
