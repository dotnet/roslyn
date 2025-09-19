// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ForEachCast;

internal static class ForEachCastHelpers
{
    public const string IsFixable = nameof(IsFixable);
}

internal abstract class AbstractForEachCastDiagnosticAnalyzer<TSyntaxKind, TForEachStatementSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct, Enum
    where TForEachStatementSyntax : SyntaxNode
{
    public static readonly ImmutableDictionary<string, string?> s_isFixableProperties =
        ImmutableDictionary<string, string?>.Empty.Add(ForEachCastHelpers.IsFixable, ForEachCastHelpers.IsFixable);

    protected AbstractForEachCastDiagnosticAnalyzer()
        : base(
              diagnosticId: IDEDiagnosticIds.ForEachCastDiagnosticId,
              EnforceOnBuildValues.ForEachCast,
              CodeStyleOptions2.ForEachExplicitCastInSource,
              title: new LocalizableResourceString(nameof(AnalyzersResources.Add_explicit_cast), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
              messageFormat: new LocalizableResourceString(nameof(AnalyzersResources._0_statement_implicitly_converts_1_to_2_Add_an_explicit_cast_to_make_intent_clearer_as_it_may_fail_at_runtime), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract ImmutableArray<TSyntaxKind> GetSyntaxKinds();
    protected abstract (CommonConversion conversion, ITypeSymbol? collectionElementType) GetForEachInfo(SemanticModel semanticModel, TForEachStatementSyntax node);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(context => AnalyzeSyntax(context), GetSyntaxKinds());
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        if (context.Node is not TForEachStatementSyntax node)
            return;

        var option = context.GetAnalyzerOptions().ForEachExplicitCastInSource;
        Contract.ThrowIfFalse(option.Value is ForEachExplicitCastInSourcePreference.Always or ForEachExplicitCastInSourcePreference.WhenStronglyTyped);

        if (ShouldSkipAnalysis(context, option.Notification))
            return;

        if (semanticModel.GetOperation(node, cancellationToken) is not IForEachLoopOperation loopOperation)
            return;

        if (loopOperation.LoopControlVariable is not IVariableDeclaratorOperation variableDeclarator ||
            variableDeclarator.Symbol.Type is not ITypeSymbol iterationType)
        {
            return;
        }

        var syntaxFacts = this.SyntaxFacts;
        var collectionType = semanticModel.GetTypeInfo(syntaxFacts.GetExpressionOfForeachStatement(node), cancellationToken).Type;
        if (collectionType is null)
            return;

        var (conversion, collectionElementType) = GetForEachInfo(semanticModel, node);
        if (collectionElementType is null)
            return;

        // Don't bother checking conversions that are problematic for other reasons.  The user will already have a
        // compiler error telling them the foreach is in error.
        if (!conversion.Exists)
            return;

        // Consider:
        // public class C : IEnumerable<Match>
        // {
        //     public IEnumerator GetEnumerator() => null; // compiler picks this for the foreach loop.
        //
        //     IEnumerator<Match> IEnumerable<Match>.GetEnumerator() => null; // compiler doesn't use this.
        // }

        // This collection have GetEnumerator method that returns non-strongly-typed IEnumerator, but also implements strongly-typed IEnumerable<T> explicitly.
        // In this case, the compiler chooses the non-strongly-typed GetEnumerator and adds explicit cast for `foreach (Match m in new C())`.
        // This cast can fail if the collection is badly implemented such that the strongly-typed and non-strongly-typed GetEnumerator implemetations return different types.
        // Given it's very rare that implementation can be bad, and to reduce false positives, we adjust collectionElementType for the case above to be `Match` instead of object.
        if (collectionElementType.SpecialType == SpecialType.System_Object)
        {
            var ienumerableOfT = collectionType.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
            if (ienumerableOfT is not null)
            {
                collectionElementType = ienumerableOfT.TypeArguments[0];
                conversion = context.Compilation.ClassifyCommonConversion(collectionElementType, iterationType);
            }
        }

        // If the conversion was implicit, then everything is ok.  Implicit conversions are safe and do not throw at runtime.
        if (conversion.IsImplicit)
            return;

        // An implicit legal conversion still shows up as explicit conversion in the object model.  But this is fine
        // to keep as is since being an implicit-conversion means the API indicates it should always be safe to
        // happen at runtime.
        if (conversion.IsUserDefined && conversion.MethodSymbol is { Name: WellKnownMemberNames.ImplicitConversionName })
            return;

        // We had a conversion that was explicit.  These are potentially unsafe as they can throw at runtime.
        // Generally, we would like to notify the user about these.  However, we have different policies depending
        // on if we think this is a legacy API or not.  Legacy APIs are those built before generics, and thus often
        // have APIs that will just return `objects` and thus always need some sort of cast to get them to the right
        // type.  A good example of that is S.T.RegularExpressions.CaptureCollection.  Users will almost always
        // write this was `foreach (Capture capture in match.Captures)` and it would be annoying to force them to
        // change this.
        //
        // What we do want to warn on are things like: `foreach (IUnrelatedInterface iface in stronglyTypedCollection)`.
        //
        // So, to detect if we're in a legacy situation, we look for iterations that are returning an object-type
        // where the collection itself didn't implement `IEnumerable<T>` in some way.
        if (option.Value == ForEachExplicitCastInSourcePreference.WhenStronglyTyped &&
            !IsStronglyTyped(collectionType, collectionElementType))
        {
            return;
        }

        // The user either always wants to write these casts explicitly, or they were calling a non-legacy API.
        // report the issue so they can insert the appropriate cast.

        // We can only fix this issue if the collection type implemented ienumerable and we have
        // System.Linq.Enumerable available.  Then we can add a .Cast call to their collection explicitly.
        var isFixable = collectionType.SpecialType == SpecialType.System_Collections_IEnumerable || collectionType.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_Collections_IEnumerable) &&
            semanticModel.Compilation.GetBestTypeByMetadataName(typeof(Enumerable).FullName!) != null;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            node.GetFirstToken().GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: null,
            properties: isFixable ? s_isFixableProperties : null,
            node.GetFirstToken().ToString(),
            collectionElementType.ToDisplayString(),
            iterationType.ToDisplayString()));
    }

    private static bool IsStronglyTyped(ITypeSymbol collectionType, ITypeSymbol collectionElementType)
        => collectionElementType.SpecialType != SpecialType.System_Object ||
           collectionType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
           collectionType.AllInterfaces.Any(static i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
}
