// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// This Object contains the options that needs to be drilled down to the Simplification Engine
    /// </summary>
    public class SimplificationOptions
    {
        internal const string FeatureName = "Simplification";

        /// <summary>
        /// This option tells the simplification engine if the Qualified Name should be replaced by Alias
        /// if the user had initially not used the Alias
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> PreferAliasToQualification = new Option<bool>(FeatureName, "PreferAliasToQualification", true);

        /// <summary>
        /// This option influences the name reduction of members of a module in VB. If set to true, the 
        /// name reducer will e.g. reduce Namespace.Module.Member to Namespace.Member.
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> PreferOmittingModuleNamesInQualification = new Option<bool>(FeatureName, "PreferOmittingModuleNamesInQualification", true);

        /// <summary>
        /// This option says that if we should simplify the Generic Name which has the type argument inferred
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> PreferImplicitTypeInference = new Option<bool>(FeatureName, "PreferImplicitTypeInference", true);

        /// <summary>
        /// This option says if we should simplify the Explicit Type in Local Declarations
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> PreferImplicitTypeInLocalDeclaration = new Option<bool>(FeatureName, "PreferImplicitTypeInLocalDeclaration", false);

        /// <summary>
        /// This option says if we should simplify to NonGeneric Name rather than GenericName
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> AllowSimplificationToGenericType = new Option<bool>(FeatureName, "AllowSimplificationToGenericType", false);

        /// <summary>
        /// This option says if we should simplify from Derived types to Base types in Static Member Accesses
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> AllowSimplificationToBaseType = new Option<bool>(FeatureName, "AllowSimplificationToBaseType", true);

        /// <summary>
        /// This option says if we should simplify away the this. or Me. in member access expression
        /// </summary>
#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<bool> QualifyMemberAccessWithThisOrMe = new PerLanguageOption<bool>(FeatureName, "QualifyMemberAccessWithThisOrMe", defaultValue: false);
    }
}
