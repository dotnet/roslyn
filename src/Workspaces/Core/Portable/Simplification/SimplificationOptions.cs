// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>, PerLanguageOption<T>

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification;

/// <summary>
/// Contains the options that needs to be drilled down to the Simplification Engine
/// </summary>
public static class SimplificationOptions
{
    /// <summary>
    /// This option tells the simplification engine if the Qualified Name should be replaced by Alias
    /// if the user had initially not used the Alias
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> PreferAliasToQualification { get; } = new Option<bool>("SimplificationOptions", "PreferAliasToQualification", defaultValue: true);

    /// <summary>
    /// This option influences the name reduction of members of a module in VB. If set to true, the 
    /// name reducer will e.g. reduce Namespace.Module.Member to Namespace.Member.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> PreferOmittingModuleNamesInQualification { get; } = new Option<bool>("SimplificationOptions", "PreferOmittingModuleNamesInQualification", defaultValue: true);

    /// <summary>
    /// This option says that if we should simplify the Generic Name which has the type argument inferred
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> PreferImplicitTypeInference { get; } = new Option<bool>("SimplificationOptions", "PreferImplicitTypeInference", defaultValue: true);

    /// <summary>
    /// This option says if we should simplify the Explicit Type in Local Declarations
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> PreferImplicitTypeInLocalDeclaration { get; } = new Option<bool>("SimplificationOptions", "PreferImplicitTypeInLocalDeclaration", defaultValue: true);

    /// <summary>
    /// This option says if we should simplify to NonGeneric Name rather than GenericName
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> AllowSimplificationToGenericType { get; } = new Option<bool>("SimplificationOptions", "AllowSimplificationToGenericType", defaultValue: false);

    /// <summary>
    /// This option says if we should simplify from Derived types to Base types in Static Member Accesses
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static Option<bool> AllowSimplificationToBaseType { get; } = new Option<bool>("SimplificationOptions", "AllowSimplificationToBaseType", defaultValue: true);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/> or <see langword="Me"/> in member access expressions.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> QualifyMemberAccessWithThisOrMe { get; } = new PerLanguageOption<bool>("SimplificationOptions", "QualifyMemberAccessWithThisOrMe", defaultValue: false);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> QualifyFieldAccess { get; } = new PerLanguageOption<bool>("SimplificationOptions", "QualifyFieldAccess", defaultValue: false);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> QualifyPropertyAccess { get; } = new("SimplificationOptions", "QualifyPropertyAccess", defaultValue: false);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> QualifyMethodAccess { get; } = new("SimplificationOptions", "QualifyMethodAccess", defaultValue: false);

    /// <summary>
    /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> QualifyEventAccess { get; } = new("SimplificationOptions", "QualifyEventAccess", defaultValue: false);

    /// <summary>
    /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration { get; } = new("SimplificationOptions", "PreferIntrinsicPredefinedTypeKeywordInDeclaration", defaultValue: true);

    /// <summary>
    /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
    /// </summary>
    [Obsolete("This option is no longer used")]
    public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess { get; } = new("SimplificationOptions", "PreferIntrinsicPredefinedTypeKeywordInMemberAccess", defaultValue: true);
}
