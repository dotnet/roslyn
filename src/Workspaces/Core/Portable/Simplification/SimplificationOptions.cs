// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
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
        public static Option<bool> PreferAliasToQualification { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferAliasToQualification), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferAliasToQualification"));

        /// <summary>
        /// This option influences the name reduction of members of a module in VB. If set to true, the 
        /// name reducer will e.g. reduce Namespace.Module.Member to Namespace.Member.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static Option<bool> PreferOmittingModuleNamesInQualification { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferOmittingModuleNamesInQualification), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferOmittingModuleNamesInQualification"));

        /// <summary>
        /// This option says that if we should simplify the Generic Name which has the type argument inferred
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static Option<bool> PreferImplicitTypeInference { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferImplicitTypeInference), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferImplicitTypeInference"));

        /// <summary>
        /// This option says if we should simplify the Explicit Type in Local Declarations
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static Option<bool> PreferImplicitTypeInLocalDeclaration { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(PreferImplicitTypeInLocalDeclaration), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.PreferImplicitTypeInLocalDeclaration"));

        /// <summary>
        /// This option says if we should simplify to NonGeneric Name rather than GenericName
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static Option<bool> AllowSimplificationToGenericType { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(AllowSimplificationToGenericType), false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AllowSimplificationToGenericType"));

        /// <summary>
        /// This option says if we should simplify from Derived types to Base types in Static Member Accesses
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static Option<bool> AllowSimplificationToBaseType { get; } = new Option<bool>(nameof(SimplificationOptions), nameof(AllowSimplificationToBaseType), true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.AllowSimplificationToBaseType"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/> or <see langword="Me"/> in member access expressions.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> QualifyMemberAccessWithThisOrMe { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyMemberAccessWithThisOrMe), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMemberAccessWithThisOrMe"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> QualifyFieldAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyFieldAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyFieldAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> QualifyPropertyAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyPropertyAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyPropertyAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> QualifyMethodAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyMethodAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyMethodAccess"));

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> QualifyEventAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(QualifyEventAccess), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.QualifyEventAccess"));

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration), defaultValue: true);

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        [Obsolete("This option is no longer used")]
        public static PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess { get; } = new PerLanguageOption<bool>(nameof(SimplificationOptions), nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess), defaultValue: true);
    }
}
