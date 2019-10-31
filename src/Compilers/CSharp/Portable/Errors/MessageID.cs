// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum MessageID
    {
        None = 0,
        MessageBase = 1200,

        IDS_SK_METHOD = MessageBase + 2000,
        IDS_SK_TYPE = MessageBase + 2001,
        IDS_SK_NAMESPACE = MessageBase + 2002,
        IDS_SK_FIELD = MessageBase + 2003,
        IDS_SK_PROPERTY = MessageBase + 2004,
        IDS_SK_UNKNOWN = MessageBase + 2005,
        IDS_SK_VARIABLE = MessageBase + 2006,
        IDS_SK_EVENT = MessageBase + 2007,
        IDS_SK_TYVAR = MessageBase + 2008,
        //IDS_SK_GCLASS = MessageBase + 2009,
        IDS_SK_ALIAS = MessageBase + 2010,
        //IDS_SK_EXTERNALIAS = MessageBase + 2011,
        IDS_SK_LABEL = MessageBase + 2012,
        IDS_SK_CONSTRUCTOR = MessageBase + 2013,

        IDS_NULL = MessageBase + 10001,
        //IDS_RELATEDERROR = MessageBase + 10002,
        //IDS_RELATEDWARNING = MessageBase + 10003,
        IDS_XMLIGNORED = MessageBase + 10004,
        IDS_XMLIGNORED2 = MessageBase + 10005,
        IDS_XMLFAILEDINCLUDE = MessageBase + 10006,
        IDS_XMLBADINCLUDE = MessageBase + 10007,
        IDS_XMLNOINCLUDE = MessageBase + 10008,
        IDS_XMLMISSINGINCLUDEFILE = MessageBase + 10009,
        IDS_XMLMISSINGINCLUDEPATH = MessageBase + 10010,
        IDS_GlobalNamespace = MessageBase + 10011,
        IDS_FeatureGenerics = MessageBase + 12500,
        IDS_FeatureAnonDelegates = MessageBase + 12501,
        IDS_FeatureModuleAttrLoc = MessageBase + 12502,
        IDS_FeatureGlobalNamespace = MessageBase + 12503,
        IDS_FeatureFixedBuffer = MessageBase + 12504,
        IDS_FeaturePragma = MessageBase + 12505,
        IDS_FOREACHLOCAL = MessageBase + 12506,
        IDS_USINGLOCAL = MessageBase + 12507,
        IDS_FIXEDLOCAL = MessageBase + 12508,
        IDS_FeatureStaticClasses = MessageBase + 12511,
        IDS_FeaturePartialTypes = MessageBase + 12512,
        IDS_MethodGroup = MessageBase + 12513,
        IDS_AnonMethod = MessageBase + 12514,
        IDS_FeatureSwitchOnBool = MessageBase + 12517,
        //IDS_WarnAsError = MessageBase + 12518,
        IDS_Collection = MessageBase + 12520,
        IDS_FeaturePropertyAccessorMods = MessageBase + 12522,
        IDS_FeatureExternAlias = MessageBase + 12523,
        IDS_FeatureIterators = MessageBase + 12524,
        IDS_FeatureDefault = MessageBase + 12525,
        IDS_FeatureNullable = MessageBase + 12528,
        IDS_Lambda = MessageBase + 12531,
        IDS_FeaturePatternMatching = MessageBase + 12532,
        IDS_FeatureThrowExpression = MessageBase + 12533,

        IDS_FeatureImplicitArray = MessageBase + 12557,
        IDS_FeatureImplicitLocal = MessageBase + 12558,
        IDS_FeatureAnonymousTypes = MessageBase + 12559,
        IDS_FeatureAutoImplementedProperties = MessageBase + 12560,
        IDS_FeatureObjectInitializer = MessageBase + 12561,
        IDS_FeatureCollectionInitializer = MessageBase + 12562,
        IDS_FeatureLambda = MessageBase + 12563,
        IDS_FeatureQueryExpression = MessageBase + 12564,
        IDS_FeatureExtensionMethod = MessageBase + 12565,
        IDS_FeaturePartialMethod = MessageBase + 12566,
        IDS_FeatureDynamic = MessageBase + 12644,
        IDS_FeatureTypeVariance = MessageBase + 12645,
        IDS_FeatureNamedArgument = MessageBase + 12646,
        IDS_FeatureOptionalParameter = MessageBase + 12647,
        IDS_FeatureExceptionFilter = MessageBase + 12648,
        IDS_FeatureAutoPropertyInitializer = MessageBase + 12649,

        IDS_SK_TYPE_OR_NAMESPACE = MessageBase + 12652,
        IDS_Contravariant = MessageBase + 12659,
        IDS_Contravariantly = MessageBase + 12660,
        IDS_Covariant = MessageBase + 12661,
        IDS_Covariantly = MessageBase + 12662,
        IDS_Invariantly = MessageBase + 12663,

        IDS_FeatureAsync = MessageBase + 12668,
        IDS_FeatureStaticAnonymousFunction = MessageBase + 12669,

        IDS_LIB_ENV = MessageBase + 12680,
        IDS_LIB_OPTION = MessageBase + 12681,
        IDS_REFERENCEPATH_OPTION = MessageBase + 12682,
        IDS_DirectoryDoesNotExist = MessageBase + 12683,
        IDS_DirectoryHasInvalidPath = MessageBase + 12684,

        IDS_Namespace1 = MessageBase + 12685,
        IDS_PathList = MessageBase + 12686,
        IDS_Text = MessageBase + 12687,

        // available

        IDS_FeatureNullPropagatingOperator = MessageBase + 12690,
        IDS_FeatureExpressionBodiedMethod = MessageBase + 12691,
        IDS_FeatureExpressionBodiedProperty = MessageBase + 12692,
        IDS_FeatureExpressionBodiedIndexer = MessageBase + 12693,
        // IDS_VersionExperimental = MessageBase + 12694,
        IDS_FeatureNameof = MessageBase + 12695,
        IDS_FeatureDictionaryInitializer = MessageBase + 12696,

        IDS_ToolName = MessageBase + 12697,
        IDS_LogoLine1 = MessageBase + 12698,
        IDS_LogoLine2 = MessageBase + 12699,
        IDS_CSCHelp = MessageBase + 12700,

        IDS_FeatureUsingStatic = MessageBase + 12701,
        IDS_FeatureInterpolatedStrings = MessageBase + 12702,
        IDS_OperationCausedStackOverflow = MessageBase + 12703,
        IDS_AwaitInCatchAndFinally = MessageBase + 12704,
        IDS_FeatureReadonlyAutoImplementedProperties = MessageBase + 12705,
        IDS_FeatureBinaryLiteral = MessageBase + 12706,
        IDS_FeatureDigitSeparator = MessageBase + 12707,
        IDS_FeatureLocalFunctions = MessageBase + 12708,
        IDS_FeatureNullableReferenceTypes = MessageBase + 12709,

        IDS_FeatureRefLocalsReturns = MessageBase + 12710,
        IDS_FeatureTuples = MessageBase + 12711,
        IDS_FeatureOutVar = MessageBase + 12713,

        // IDS_FeaturePragmaWarningEnable = MessageBase + 12714,
        IDS_FeatureExpressionBodiedAccessor = MessageBase + 12715,
        IDS_FeatureExpressionBodiedDeOrConstructor = MessageBase + 12716,
        IDS_ThrowExpression = MessageBase + 12717,
        IDS_FeatureDefaultLiteral = MessageBase + 12718,
        IDS_FeatureInferredTupleNames = MessageBase + 12719,
        IDS_FeatureGenericPatternMatching = MessageBase + 12720,
        IDS_FeatureAsyncMain = MessageBase + 12721,
        IDS_LangVersions = MessageBase + 12722,

        IDS_FeatureLeadingDigitSeparator = MessageBase + 12723,
        IDS_FeatureNonTrailingNamedArguments = MessageBase + 12724,

        IDS_FeatureReadOnlyReferences = MessageBase + 12725,
        IDS_FeatureRefStructs = MessageBase + 12726,
        IDS_FeatureReadOnlyStructs = MessageBase + 12727,
        IDS_FeatureRefExtensionMethods = MessageBase + 12728,
        // IDS_StackAllocExpression = MessageBase + 12729,
        IDS_FeaturePrivateProtected = MessageBase + 12730,

        IDS_FeatureRefConditional = MessageBase + 12731,
        IDS_FeatureAttributesOnBackingFields = MessageBase + 12732,
        IDS_FeatureImprovedOverloadCandidates = MessageBase + 12733,
        IDS_FeatureRefReassignment = MessageBase + 12734,
        IDS_FeatureRefFor = MessageBase + 12735,
        IDS_FeatureRefForEach = MessageBase + 12736,
        IDS_FeatureEnumGenericTypeConstraint = MessageBase + 12737,
        IDS_FeatureDelegateGenericTypeConstraint = MessageBase + 12738,
        IDS_FeatureUnmanagedGenericTypeConstraint = MessageBase + 12739,
        IDS_FeatureStackAllocInitializer = MessageBase + 12740,
        IDS_FeatureTupleEquality = MessageBase + 12741,
        IDS_FeatureExpressionVariablesInQueriesAndInitializers = MessageBase + 12742,
        IDS_FeatureExtensibleFixedStatement = MessageBase + 12743,
        IDS_FeatureIndexingMovableFixedBuffers = MessageBase + 12744,

        IDS_FeatureAltInterpolatedVerbatimStrings = MessageBase + 12745,
        IDS_FeatureCoalesceAssignmentExpression = MessageBase + 12746,
        IDS_FeatureUnconstrainedTypeParameterInNullCoalescingOperator = MessageBase + 12747,
        IDS_FeatureNotNullGenericTypeConstraint = MessageBase + 12748,
        IDS_FeatureIndexOperator = MessageBase + 12749,
        IDS_FeatureRangeOperator = MessageBase + 12750,
        IDS_FeatureAsyncStreams = MessageBase + 12751,
        IDS_FeatureRecursivePatterns = MessageBase + 12752,
        IDS_Disposable = MessageBase + 12753,
        IDS_FeatureUsingDeclarations = MessageBase + 12754,
        IDS_FeatureStaticLocalFunctions = MessageBase + 12755,
        IDS_FeatureNameShadowingInNestedFunctions = MessageBase + 12756,
        IDS_FeatureUnmanagedConstructedTypes = MessageBase + 12757,
        IDS_FeatureObsoleteOnPropertyAccessor = MessageBase + 12758,
        IDS_FeatureReadOnlyMembers = MessageBase + 12759,
        IDS_DefaultInterfaceImplementation = MessageBase + 12760,
        IDS_OverrideWithConstraints = MessageBase + 12761,
        IDS_FeatureNestedStackalloc = MessageBase + 12762,
        IDS_FeatureSwitchExpression = MessageBase + 12763,
        IDS_FeatureAsyncUsing = MessageBase + 12764,
    }

    // Message IDs may refer to strings that need to be localized.
    // This struct makes an IFormattable wrapper around a MessageID
    internal struct LocalizableErrorArgument : IFormattable
    {
        private readonly MessageID _id;

        internal LocalizableErrorArgument(MessageID id)
        {
            _id = id;
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ErrorFacts.GetMessage(_id, formatProvider as System.Globalization.CultureInfo);
        }
    }

    // And this extension method makes it easy to localize MessageIDs:

    internal static partial class MessageIDExtensions
    {
        public static LocalizableErrorArgument Localize(this MessageID id)
        {
            return new LocalizableErrorArgument(id);
        }

        // Returns the string to be used in the /features flag switch to enable the MessageID feature.
        // Always call this before RequiredVersion:
        //   If this method returns null, call RequiredVersion and use that.
        //   If this method returns non-null, use that.
        // Features should be mutually exclusive between RequiredFeature and RequiredVersion.
        //   (hence the above rule - RequiredVersion throws when RequiredFeature returns non-null)
        internal static string RequiredFeature(this MessageID feature)
        {
            // Check for current experimental features, if any, in the current branch.
            switch (feature)
            {
                default:
                    return null;
            }
        }

        internal static bool CheckFeatureAvailability(
            this MessageID feature,
            DiagnosticBag diagnostics,
            SyntaxNode syntax,
            Location location = null)
        {
            var diag = GetFeatureAvailabilityDiagnosticInfoOpt(feature, (CSharpParseOptions)syntax.SyntaxTree.Options);
            if (diag is object)
            {
                diagnostics.Add(diag, location ?? syntax.GetLocation());
                return false;
            }
            return true;
        }

        internal static bool CheckFeatureAvailability(
            this MessageID feature,
            DiagnosticBag diagnostics,
            Compilation compilation,
            Location location)
        {
            if (GetFeatureAvailabilityDiagnosticInfoOpt(feature, (CSharpCompilation)compilation) is { } diagInfo)
            {
                diagnostics.Add(diagInfo, location);
                return false;
            }
            return true;
        }

        internal static CSDiagnosticInfo GetFeatureAvailabilityDiagnosticInfoOpt(this MessageID feature, CSharpParseOptions options)
            => options.IsFeatureEnabled(feature) ? null : GetDisabledFeatureDiagnosticInfo(feature, options.LanguageVersion);

        internal static CSDiagnosticInfo GetFeatureAvailabilityDiagnosticInfoOpt(this MessageID feature, CSharpCompilation compilation)
            => compilation.IsFeatureEnabled(feature) ? null : GetDisabledFeatureDiagnosticInfo(feature, compilation.LanguageVersion);

        private static CSDiagnosticInfo GetDisabledFeatureDiagnosticInfo(MessageID feature, LanguageVersion availableVersion)
        {
            string requiredFeature = feature.RequiredFeature();
            if (requiredFeature != null)
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_FeatureIsExperimental, feature.Localize(), requiredFeature);
            }

            LanguageVersion requiredVersion = feature.RequiredVersion();
            return requiredVersion == LanguageVersion.Preview.MapSpecifiedToEffectiveVersion()
                ? new CSDiagnosticInfo(ErrorCode.ERR_FeatureInPreview, feature.Localize())
                : new CSDiagnosticInfo(availableVersion.GetErrorCode(), feature.Localize(), new CSharpRequiredLanguageVersion(requiredVersion));
        }


        internal static LanguageVersion RequiredVersion(this MessageID feature)
        {
            Debug.Assert(RequiredFeature(feature) == null);

            // Based on CSourceParser::GetFeatureUsage from SourceParser.cpp.
            // Checks are in the LanguageParser unless otherwise noted.
            switch (feature)
            {
                case MessageID.IDS_FeatureStaticAnonymousFunction:
                    return LanguageVersion.Preview;

                // C# 8.0 features.
                case MessageID.IDS_FeatureAltInterpolatedVerbatimStrings:
                case MessageID.IDS_FeatureCoalesceAssignmentExpression:
                case MessageID.IDS_FeatureUnconstrainedTypeParameterInNullCoalescingOperator:
                case MessageID.IDS_FeatureNullableReferenceTypes: // syntax and semantic check
                case MessageID.IDS_FeatureIndexOperator: // semantic check
                case MessageID.IDS_FeatureRangeOperator: // semantic check
                case MessageID.IDS_FeatureAsyncStreams:
                case MessageID.IDS_FeatureRecursivePatterns:
                case MessageID.IDS_FeatureUsingDeclarations:
                case MessageID.IDS_FeatureStaticLocalFunctions:
                case MessageID.IDS_FeatureNameShadowingInNestedFunctions:
                case MessageID.IDS_FeatureUnmanagedConstructedTypes: // semantic check
                case MessageID.IDS_FeatureObsoleteOnPropertyAccessor:
                case MessageID.IDS_FeatureReadOnlyMembers:
                case MessageID.IDS_DefaultInterfaceImplementation: // semantic check
                case MessageID.IDS_OverrideWithConstraints: // semantic check
                case MessageID.IDS_FeatureNestedStackalloc: // semantic check
                case MessageID.IDS_FeatureNotNullGenericTypeConstraint:// semantic check
                case MessageID.IDS_FeatureSwitchExpression:
                case MessageID.IDS_FeatureAsyncUsing:
                    return LanguageVersion.CSharp8;

                // C# 7.3 features.
                case MessageID.IDS_FeatureAttributesOnBackingFields: // semantic check
                case MessageID.IDS_FeatureImprovedOverloadCandidates: // semantic check
                case MessageID.IDS_FeatureTupleEquality: // semantic check
                case MessageID.IDS_FeatureRefReassignment:
                case MessageID.IDS_FeatureRefFor:
                case MessageID.IDS_FeatureRefForEach:
                case MessageID.IDS_FeatureEnumGenericTypeConstraint: // semantic check
                case MessageID.IDS_FeatureDelegateGenericTypeConstraint: // semantic check
                case MessageID.IDS_FeatureUnmanagedGenericTypeConstraint: // semantic check
                case MessageID.IDS_FeatureStackAllocInitializer:
                case MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers: // semantic check
                case MessageID.IDS_FeatureExtensibleFixedStatement:  // semantic check
                case MessageID.IDS_FeatureIndexingMovableFixedBuffers: //semantic check
                    return LanguageVersion.CSharp7_3;

                // C# 7.2 features.
                case MessageID.IDS_FeatureNonTrailingNamedArguments: // semantic check
                case MessageID.IDS_FeatureLeadingDigitSeparator:
                case MessageID.IDS_FeaturePrivateProtected:
                case MessageID.IDS_FeatureReadOnlyReferences:
                case MessageID.IDS_FeatureRefStructs:
                case MessageID.IDS_FeatureReadOnlyStructs:
                case MessageID.IDS_FeatureRefExtensionMethods:
                case MessageID.IDS_FeatureRefConditional:
                    return LanguageVersion.CSharp7_2;

                // C# 7.1 features.
                case MessageID.IDS_FeatureAsyncMain:
                case MessageID.IDS_FeatureDefaultLiteral:
                case MessageID.IDS_FeatureInferredTupleNames:
                case MessageID.IDS_FeatureGenericPatternMatching:
                    return LanguageVersion.CSharp7_1;

                // C# 7 features.
                case MessageID.IDS_FeatureBinaryLiteral:
                case MessageID.IDS_FeatureDigitSeparator:
                case MessageID.IDS_FeatureLocalFunctions:
                case MessageID.IDS_FeatureRefLocalsReturns:
                case MessageID.IDS_FeaturePatternMatching:
                case MessageID.IDS_FeatureThrowExpression:
                case MessageID.IDS_FeatureTuples:
                case MessageID.IDS_FeatureOutVar:
                case MessageID.IDS_FeatureExpressionBodiedAccessor:
                case MessageID.IDS_FeatureExpressionBodiedDeOrConstructor:
                    return LanguageVersion.CSharp7;

                // C# 6 features.
                case MessageID.IDS_FeatureExceptionFilter:
                case MessageID.IDS_FeatureAutoPropertyInitializer:
                case MessageID.IDS_FeatureNullPropagatingOperator:
                case MessageID.IDS_FeatureExpressionBodiedMethod:
                case MessageID.IDS_FeatureExpressionBodiedProperty:
                case MessageID.IDS_FeatureExpressionBodiedIndexer:
                case MessageID.IDS_FeatureNameof:
                case MessageID.IDS_FeatureDictionaryInitializer:
                case MessageID.IDS_FeatureUsingStatic:
                case MessageID.IDS_FeatureInterpolatedStrings:
                case MessageID.IDS_AwaitInCatchAndFinally:
                case MessageID.IDS_FeatureReadonlyAutoImplementedProperties:
                    return LanguageVersion.CSharp6;

                // C# 5 features.
                case MessageID.IDS_FeatureAsync:
                    return LanguageVersion.CSharp5;

                // C# 4 features.
                case MessageID.IDS_FeatureDynamic: // Checked in the binder.
                case MessageID.IDS_FeatureTypeVariance:
                case MessageID.IDS_FeatureNamedArgument:
                case MessageID.IDS_FeatureOptionalParameter:
                    return LanguageVersion.CSharp4;

                // C# 3 features.
                case MessageID.IDS_FeatureImplicitArray:
                case MessageID.IDS_FeatureAnonymousTypes:
                case MessageID.IDS_FeatureObjectInitializer:
                case MessageID.IDS_FeatureCollectionInitializer:
                case MessageID.IDS_FeatureLambda:
                case MessageID.IDS_FeatureQueryExpression:
                case MessageID.IDS_FeatureExtensionMethod:
                case MessageID.IDS_FeaturePartialMethod:
                case MessageID.IDS_FeatureImplicitLocal: // Checked in the binder.
                case MessageID.IDS_FeatureAutoImplementedProperties:
                    return LanguageVersion.CSharp3;

                // C# 2 features.
                case MessageID.IDS_FeatureGenerics: // Also affects crefs.
                case MessageID.IDS_FeatureAnonDelegates:
                case MessageID.IDS_FeatureGlobalNamespace: // Also affects crefs.
                case MessageID.IDS_FeatureFixedBuffer:
                case MessageID.IDS_FeatureStaticClasses:
                case MessageID.IDS_FeaturePartialTypes:
                case MessageID.IDS_FeaturePropertyAccessorMods:
                case MessageID.IDS_FeatureExternAlias:
                case MessageID.IDS_FeatureIterators:
                case MessageID.IDS_FeatureDefault:
                case MessageID.IDS_FeatureNullable:
                case MessageID.IDS_FeaturePragma: // Checked in the directive parser.
                case MessageID.IDS_FeatureSwitchOnBool: // Checked in the binder.
                    return LanguageVersion.CSharp2;

                // Special C# 2 feature: only a warning in C# 1.
                case MessageID.IDS_FeatureModuleAttrLoc:
                    return LanguageVersion.CSharp1;

                default:
                    throw ExceptionUtilities.UnexpectedValue(feature);
            }
        }
    }
}
