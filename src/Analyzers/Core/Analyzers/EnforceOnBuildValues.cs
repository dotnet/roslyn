// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class EnforceOnBuildValues
{
    /* EnforceOnBuild.HighlyRecommended */
    public const EnforceOnBuild RemoveUnnecessaryImports = /*IDE0005*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UseImplicitType = /*IDE0007*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UseExplicitType = /*IDE0008*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild AddBraces = /*IDE0011*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild OrderModifiers = /*IDE0036*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild AddAccessibilityModifiers = /*IDE0040*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild ValidateFormatString = /*IDE0043*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild MakeFieldReadonly = /*IDE0044*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild RemoveUnusedMembers = /*IDE0051*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild RemoveUnreadMembers = /*IDE0052*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild Formatting = /*IDE0055*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild ValueAssignedIsUnused = /*IDE0059*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UnusedParameter = /*IDE0060*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild FileHeaderMismatch = /*IDE0073*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild InvalidSuppressMessageAttribute = /*IDE0076*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild LegacyFormatSuppressMessageAttribute = /*IDE0077*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild RemoveConfusingSuppressionForIsExpression = /*IDE0080*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UseBlockScopedNamespace = /*IDE0160*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UseFileScopedNamespace = /*IDE0161*/ EnforceOnBuild.HighlyRecommended;
    public const EnforceOnBuild UseTupleSwap = /*IDE0180*/ EnforceOnBuild.HighlyRecommended;

    /* EnforceOnBuild.Recommended */
    public const EnforceOnBuild UseThrowExpression = /*IDE0016*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseObjectInitializer = /*IDE0017*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild InlineDeclaration = /*IDE0018*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild InlineAsType = /*IDE0019*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild InlineIsType = /*IDE0020*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForConstructors = /*IDE0021*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForMethods = /*IDE0022*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForConversionOperators = /*IDE0023*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForOperators = /*IDE0024*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForProperties = /*IDE0025*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForIndexers = /*IDE0026*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForAccessors = /*IDE0027*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionInitializer = /*IDE0028*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCoalesceExpression = /*IDE0029*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCoalesceExpressionForNullable = /*IDE0030*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseNullPropagation = /*IDE0031*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseAutoProperty = /*IDE0032*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExplicitTupleName = /*IDE0033*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseDefaultLiteral = /*IDE0034*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild InlineIsTypeWithoutName = /*IDE0038*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseLocalFunction = /*IDE0039*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseDeconstruction = /*IDE0042*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseConditionalExpressionForAssignment = /*IDE0045*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseConditionalExpressionForReturn = /*IDE0046*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveUnnecessaryParentheses = /*IDE0047*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForLambdaExpressions = /*IDE0053*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCompoundAssignment = /*IDE0054*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseIndexOperator = /*IDE0056*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseRangeOperator = /*IDE0057*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseExpressionBodyForLocalFunctions = /*IDE0061*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild MakeLocalFunctionStatic = /*IDE0062*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseSimpleUsingStatement = /*IDE0063*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild MoveMisplacedUsingDirectives = /*IDE0065*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseSystemHashCode = /*IDE0070*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild SimplifyInterpolation = /*IDE0071*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCoalesceCompoundAssignment = /*IDE0074*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild SimplifyConditionalExpression = /*IDE0075*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UsePatternCombinators = /*IDE0078*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveUnnecessaryByVal = /*IDE0081*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild ConvertTypeOfToNameOf = /*IDE0082*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseNotPattern = /*IDE0083*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseIsNotExpression = /*IDE0084*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseImplicitObjectCreation = /*IDE0090*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveRedundantEquality = /*IDE0100*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveUnnecessaryDiscardDesignation = /*IDE0110*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveUnnecessaryLambdaExpression = /*IDE0200*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild InvokeDelegateWithConditionalAccess = /*IDE1005*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild NamingRule = /*IDE1006*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild MatchFolderAndNamespace = /*IDE0130*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild SimplifyObjectCreation = /*IDE0140*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild SimplifyPropertyPattern = /*IDE0170*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveRedundantNullableDirective = /*IDE0240*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild RemoveUnnecessaryNullableDirective = /*IDE0241*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild MakeStructReadOnly = /*IDE0250*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild MakeStructMemberReadOnly = /*IDE0251*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UsePatternMatchingAsAndMemberAccess = /*IDE0260*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCoalesceExpressionForIfNullCheck = /*IDE0270*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseNameofInAttribute = /*IDE0280*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UsePrimaryConstructor = /*IDE0290*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForArray = /*IDE0300*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForEmpty = /*IDE0301*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForStackAlloc = /*IDE0302*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForCreate = /*IDE0303*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForBuilder = /*IDE0304*/ EnforceOnBuild.Recommended;
    public const EnforceOnBuild UseCollectionExpressionForFluent = /*IDE0305*/ EnforceOnBuild.Recommended;

    /* EnforceOnBuild.WhenExplicitlyEnabled */
    public const EnforceOnBuild RemoveUnnecessaryCast = /*IDE0004*/ EnforceOnBuild.WhenExplicitlyEnabled; // TODO: Move to 'Recommended' OR 'HighlyRecommended' bucket once performance problems are addressed: https://github.com/dotnet/roslyn/issues/43304
    public const EnforceOnBuild PopulateSwitchStatement = /*IDE0010*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseInferredMemberName = /*IDE0037*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseIsNullCheck = /*IDE0041*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild AddRequiredParentheses = /*IDE0048*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ExpressionValueIsUnused = /*IDE0058*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild MakeStructFieldsWritable = /*IDE0064*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ConvertSwitchStatementToExpression = /*IDE0066*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild PopulateSwitchExpression = /*IDE0072*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild SimplifyLinqExpression = /*IDE0120*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseNullCheckOverTypeCheck = /*IDE0150*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseTopLevelStatements = /*IDE0210*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseProgramMain = /*IDE0211*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ForEachCast = /*IDE0220*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild UseUtf8StringLiteral = /*IDE0230*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild MultipleBlankLines = /*IDE2000*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild EmbeddedStatementPlacement = /*IDE2001*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ConsecutiveBracePlacement = /*IDE2002*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ConsecutiveStatementPlacement = /*IDE2003*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ConstructorInitializerPlacement = /*IDE2004*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ConditionalExpressionPlacement = /*IDE2005*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild ArrowExpressionClausePlacement = /*IDE2006*/ EnforceOnBuild.WhenExplicitlyEnabled;

    public const EnforceOnBuild Regex = /*RE0001*/ EnforceOnBuild.WhenExplicitlyEnabled;
    public const EnforceOnBuild Json = /*JSON001*/ EnforceOnBuild.WhenExplicitlyEnabled;

    /* EnforceOnBuild.Never */
    // TODO: Allow enforcing simplify names and related diagnostics on build once we validate their performance charactericstics.
    public const EnforceOnBuild SimplifyNames = /*IDE0001*/ EnforceOnBuild.Never;
    public const EnforceOnBuild SimplifyMemberAccess = /*IDE0002*/ EnforceOnBuild.Never;
    public const EnforceOnBuild RemoveQualification = /*IDE0003*/ EnforceOnBuild.Never;
    public const EnforceOnBuild AddQualification = /*IDE0009*/ EnforceOnBuild.Never;
    public const EnforceOnBuild PreferBuiltInOrFrameworkType = /*IDE0049*/ EnforceOnBuild.Never;
    public const EnforceOnBuild ConvertAnonymousTypeToTuple = /*IDE0050*/ EnforceOnBuild.Never;
    public const EnforceOnBuild RemoveUnreachableCode = /*IDE0035*/ EnforceOnBuild.Never; // Non-configurable fading diagnostic corresponding to CS0162.
    public const EnforceOnBuild RemoveUnnecessarySuppression = /*IDE0079*/ EnforceOnBuild.Never; // IDE-only analyzer.

    // Pure IDE feature for lighting up editor features.  Do not enforce on build.
    public const EnforceOnBuild DetectProbableJsonStrings = /*JSON002*/ EnforceOnBuild.Never;
}
