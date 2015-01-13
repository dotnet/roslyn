// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// This Object contains the options that needs to be drilled down to the Simplification Engine
    /// </summary>
    public class SimplificationOptions
    {
        internal const string NonPerLanguageFeatureName = "Simplification";
        internal const string PerLanguageFeatureName = "SimplificationPerLanguage";

        /// <summary>
        /// This option tells the simplification engine if the Qualified Name should be replaced by Alias
        /// if the user had initially not used the Alias
        /// </summary>
        public static readonly Option<bool> PreferAliasToQualification = new Option<bool>(NonPerLanguageFeatureName, "PreferAliasToQualification", true);

        /// <summary>
        /// This option influences the name reduction of members of a module in VB. If set to true, the 
        /// name reducer will e.g. reduce Namespace.Module.Member to Namespace.Member.
        /// </summary>
        public static readonly Option<bool> PreferOmittingModuleNamesInQualification = new Option<bool>(NonPerLanguageFeatureName, "PreferOmittingModuleNamesInQualification", true);

        /// <summary>
        /// This option says that if we should simplify the Generic Name which has the type argument inferred
        /// </summary>
        public static readonly Option<bool> PreferImplicitTypeInference = new Option<bool>(NonPerLanguageFeatureName, "PreferImplicitTypeInference", true);

        /// <summary>
        /// This option says if we should simplify the Explicit Type in Local Declarations
        /// </summary>
        public static readonly Option<bool> PreferImplicitTypeInLocalDeclaration = new Option<bool>(NonPerLanguageFeatureName, "PreferImplicitTypeInLocalDeclaration", false);

        /// <summary>
        /// This option says if we should simplify to NonGeneric Name rather than GenericName
        /// </summary>
        public static readonly Option<bool> AllowSimplificationToGenericType = new Option<bool>(NonPerLanguageFeatureName, "AllowSimplificationToGenericType", false);

        /// <summary>
        /// This option says if we should simplify from Derived types to Base types in Static Member Accesses
        /// </summary>
        public static readonly Option<bool> AllowSimplificationToBaseType = new Option<bool>(NonPerLanguageFeatureName, "AllowSimplificationToBaseType", true);

        /// <summary>
        /// This option says if we should simplify away the this. or Me. in member access expression
        /// </summary>
        public static readonly PerLanguageOption<bool> QualifyMemberAccessWithThisOrMe = new PerLanguageOption<bool>(PerLanguageFeatureName, "QualifyMemberAccessWithThisOrMe", defaultValue: false);

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInDeclaration = new PerLanguageOption<bool>(PerLanguageFeatureName, "PreferIntrinsicPredefinedTypeKeywordInDeclaration", defaultValue: true);

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption<bool> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = new PerLanguageOption<bool>(PerLanguageFeatureName, "PreferIntrinsicPredefinedTypeKeywordInMemberAccess", defaultValue: true);
    }
}
