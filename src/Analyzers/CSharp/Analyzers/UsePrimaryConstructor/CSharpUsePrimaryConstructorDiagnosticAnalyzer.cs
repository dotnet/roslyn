// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: kvp

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor;

/// <summary>
/// Looks for code of the forms:
/// <code>
///     class Point
///     {
///         private int x;
///         private int y;
/// 
///         public C(int x, int y)
///         {
///             this.x = x;
///             this.y = y;
///         }
///     }
/// </code>
/// and converts it to:
/// <code>
///     class Point(int x, int y)
///     {
///     }
/// </code>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUsePrimaryConstructorDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId,
        EnforceOnBuildValues.UsePrimaryConstructor,
        CSharpCodeStyleOptions.PreferPrimaryConstructors,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Use_primary_constructor), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    // Deliberately using names that could not be actual field/property names in the properties dictionary.
    public const string AllFieldsName = "<>AllFields";
    public const string AllPropertiesName = "<>AllProperties";

    private static readonly ObjectPool<ConcurrentSet<ISymbol>> s_concurrentSetPool = new(() => []);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (!context.Compilation.LanguageVersion().SupportsPrimaryConstructors())
                return;

            // Mapping from a named type to a particular analyzer we have created for it. Needed because nested
            // types need to update the information for their containing types while they themselves are being
            // analyzed.
            var namedTypeToAnalyzer = new ConcurrentDictionary<INamedTypeSymbol, Analyzer>();
            context.RegisterSymbolStartAction(context => Analyzer.AnalyzeNamedTypeStart(this, context, namedTypeToAnalyzer), SymbolKind.NamedType);
        });
    }

    public static bool IsViableMemberToAssignTo(
        INamedTypeSymbol namedType,
        [NotNullWhen(true)] ISymbol? member,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberNode,
        [NotNullWhen(true)] out SyntaxNode? nodeToRemove,
        CancellationToken cancellationToken)
    {
        memberNode = null;
        nodeToRemove = null;
        if (member is not IFieldSymbol and not IPropertySymbol)
            return false;

        if (member.IsImplicitlyDeclared)
            return false;

        if (member.IsStatic)
            return false;

        if (!namedType.Equals(member.ContainingType))
            return false;

        if (member.DeclaringSyntaxReferences is not [var memberReference, ..])
            return false;

        nodeToRemove = memberReference.GetSyntax(cancellationToken);
        if (nodeToRemove is not VariableDeclaratorSyntax and not PropertyDeclarationSyntax)
            return false;

        // If it's a property, then it has to be an auto property in order for us to be able to initialize is
        // directly outside of a constructor.
        if (nodeToRemove is PropertyDeclarationSyntax property)
        {
            if (property.AccessorList is null ||
                property.AccessorList.Accessors.Any(static a => a.ExpressionBody != null || a.Body != null))
            {
                return false;
            }

            memberNode = property;
            return true;
        }
        else if (nodeToRemove is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax field } })
        {
            memberNode = field;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Helper type we create that encapsulates all the state we need while processing.
    /// </summary>
    private sealed class Analyzer(
        CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer,
        CodeStyleOption2<bool> styleOption,
        INamedTypeSymbol namedType,
        ConstructorDeclarationSyntax primaryConstructorDeclaration,
        PooledDictionary<ISymbol, IParameterSymbol> candidateMembersToRemove,
        ConcurrentDictionary<INamedTypeSymbol, Analyzer> namedTypeToAnalyzer)
    {
        private readonly CSharpUsePrimaryConstructorDiagnosticAnalyzer _diagnosticAnalyzer = diagnosticAnalyzer;

        private readonly CodeStyleOption2<bool> _styleOption = styleOption;
        private readonly INamedTypeSymbol _namedType = namedType;
        private readonly ConstructorDeclarationSyntax _primaryConstructorDeclaration = primaryConstructorDeclaration;

        private readonly PooledDictionary<ISymbol, IParameterSymbol> _candidateMembersToRemove = candidateMembersToRemove;
        private readonly ConcurrentDictionary<INamedTypeSymbol, Analyzer> _namedTypeToAnalyzer = namedTypeToAnalyzer;

        /// <summary>
        /// Needs to be concurrent as we can process members in parallel in <see
        /// cref="AnalyzeFieldOrPropertyReference"/>.
        /// </summary>
        private readonly ConcurrentSet<ISymbol> _membersThatCannotBeRemoved = s_concurrentSetPool.Allocate();

        public bool HasCandidateMembersToRemove => _candidateMembersToRemove.Count > 0;

        private void OnSymbolEnd(SymbolAnalysisContext context)
        {
            // Pass along a mapping of field/property name to the constructor parameter name that will replace it.
            var properties = _candidateMembersToRemove
                .Where(kvp => !_membersThatCannotBeRemoved.Contains(kvp.Key))
                .ToImmutableDictionary(
                    static kvp => kvp.Key.Name,
                    static kvp => (string?)kvp.Value.Name);

            // To provide better user-facing-strings, keep track of whether or not all the members we'd be
            // removing are all fields or all properties.
            if (_candidateMembersToRemove.Any(kvp => kvp.Key is IFieldSymbol) &&
                _candidateMembersToRemove.All(kvp => kvp.Key is IFieldSymbol))
            {
                properties = properties.Add(AllFieldsName, AllFieldsName);
            }
            else if (
                _candidateMembersToRemove.Any(kvp => kvp.Key is IPropertySymbol) &&
                _candidateMembersToRemove.All(kvp => kvp.Key is IPropertySymbol))
            {
                properties = properties.Add(AllPropertiesName, AllPropertiesName);
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                _diagnosticAnalyzer.Descriptor,
                _primaryConstructorDeclaration.Identifier.GetLocation(),
                _styleOption.Notification,
                context.Options,
                ImmutableArray.Create(_primaryConstructorDeclaration.GetLocation()),
                properties));

            _candidateMembersToRemove.Free();
            s_concurrentSetPool.ClearAndFree(_membersThatCannotBeRemoved);

            _namedTypeToAnalyzer.TryRemove(_namedType, out _);
        }

        public static void AnalyzeNamedTypeStart(
            CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer,
            SymbolStartAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, Analyzer> namedTypeToAnalyzer)
        {
            var compilation = context.Compilation;
            var cancellationToken = context.CancellationToken;

            var startSymbol = (INamedTypeSymbol)context.Symbol;
            var options = context.Options;

            // Ensure that any analyzers for containing types are created and they hear about any reference to their
            // fields in this nested type.

            for (var containingType = startSymbol.ContainingType; containingType != null; containingType = containingType.ContainingType)
            {
                var containingTypeAnalyzer = TryGetOrCreateAnalyzer(containingType);
                RegisterFieldOrPropertyAnalysisIfNecessary(containingTypeAnalyzer);
            }

            // Now try to make the analyzer for this type.
            var analyzer = TryGetOrCreateAnalyzer(startSymbol);
            if (analyzer != null)
            {
                RegisterFieldOrPropertyAnalysisIfNecessary(analyzer);
                context.RegisterSymbolEndAction(analyzer.OnSymbolEnd);
            }

            return;

            void RegisterFieldOrPropertyAnalysisIfNecessary(Analyzer? analyzer)
            {
                if (analyzer is { HasCandidateMembersToRemove: true })
                {
                    // Look to see if we have trivial `_x = x` or `this.x = x` assignments.  If so, then the field/prop
                    // could be a candidate for removal (as long as we determine that all use sites of the field/prop would
                    // be able to use the captured primary constructor parameter).
                    context.RegisterOperationAction(
                        analyzer.AnalyzeFieldOrPropertyReference,
                        OperationKind.FieldReference, OperationKind.PropertyReference);
                }
            }

            Analyzer? TryGetOrCreateAnalyzer(
                INamedTypeSymbol namedType)
            {
                if (!namedTypeToAnalyzer.TryGetValue(namedType, out var analyzer))
                {
                    analyzer = TryCreateAnalyzer(namedType);
                    if (analyzer != null)
                        namedTypeToAnalyzer.TryAdd(namedType, analyzer);

                    // If another thread beat us, defer to that.
                    namedTypeToAnalyzer.TryGetValue(namedType, out analyzer);
                }

                return analyzer;
            }

            Analyzer? TryCreateAnalyzer(INamedTypeSymbol namedType)
            {
                // Bail immediately if the user has disabled this feature.
                if (namedType.DeclaringSyntaxReferences is not [var reference, ..])
                    return null;

                var styleOption = options.GetCSharpAnalyzerOptions(reference.SyntaxTree).PreferPrimaryConstructors;
                if (!styleOption.Value
                    || diagnosticAnalyzer.ShouldSkipAnalysis(reference.SyntaxTree, context.Options, context.Compilation.Options, styleOption.Notification, cancellationToken))
                {
                    return null;
                }

                // only classes/structs can have primary constructors (not interfaces, enums or delegates).
                if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                    return null;

                // Don't want to offer on records.  It's completely fine for records to not use primary constructs and
                // instead use init-properties.
                if (namedType.IsRecord)
                    return null;

                // No need to offer this if the type already has a primary constructor.
                if (namedType.TryGetPrimaryConstructor(out _))
                    return null;

                // Need to see if there is a single constructor that either calls `base(...)` or has no constructor
                // initializer (and thus implicitly calls `base()`), and that all other constructors call `this(...)`.
                if (!TryFindPrimaryConstructorCandidate(namedType, out var primaryConstructor, out var primaryConstructorDeclaration))
                    return null;

                if (primaryConstructor.Parameters.Length == 0)
                    return null;

                // protected constructor in an abstract type is fine.  It will stay protected even as a primary constructor.
                // otherwise it has to be public.
                if (primaryConstructor.DeclaredAccessibility == Accessibility.Protected)
                {
                    if (!namedType.IsAbstract)
                        return null;
                }
                else if (primaryConstructor.DeclaredAccessibility != Accessibility.Public)
                {
                    return null;
                }

                // Constructor has to have a real body (can't be extern/etc.).
                if (primaryConstructorDeclaration is { Body: null, ExpressionBody: null })
                    return null;

                if (primaryConstructorDeclaration.Parent is not TypeDeclarationSyntax)
                    return null;

                if (primaryConstructor.Parameters.Any(static p => p.RefKind is RefKind.Ref or RefKind.Out))
                    return null;

                // Now ensure the constructor body is something that could be converted to a primary constructor (i.e.
                // only assignments to instance fields/props on this).
                var candidateMembersToRemove = PooledDictionary<ISymbol, IParameterSymbol>.GetInstance();
                if (!AnalyzeConstructorBody(namedType, primaryConstructorDeclaration, candidateMembersToRemove))
                {
                    candidateMembersToRemove.Free();
                    return null;
                }

                return new Analyzer(
                    diagnosticAnalyzer,
                    styleOption,
                    namedType,
                    primaryConstructorDeclaration,
                    candidateMembersToRemove,
                    namedTypeToAnalyzer);
            }

            bool TryFindPrimaryConstructorCandidate(
                INamedTypeSymbol namedType,
                [NotNullWhen(true)] out IMethodSymbol? primaryConstructor,
                [NotNullWhen(true)] out ConstructorDeclarationSyntax? primaryConstructorDeclaration)
            {
                primaryConstructor = null;
                primaryConstructorDeclaration = null;

                var constructors = namedType.InstanceConstructors;

                foreach (var constructor in constructors)
                {
                    // Can ignore the implicit struct constructor.  It doesn't block us making a real constructor
                    // into a primary constructor.
                    if (namedType.IsStructType() && constructor.IsImplicitlyDeclared)
                        continue;

                    // Needs to just have a single declaration
                    if (constructor.DeclaringSyntaxReferences is not [var constructorReference])
                        return false;

                    if (constructorReference.GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
                        return false;

                    if (constructorDeclaration.Initializer is null or (kind: SyntaxKind.BaseConstructorInitializer))
                    {
                        // Can only have one candidate
                        if (primaryConstructor != null)
                            return false;

                        primaryConstructor = constructor;
                        primaryConstructorDeclaration = constructorDeclaration;
                    }
                    else
                    {
                        Debug.Assert(constructorDeclaration.Initializer.Kind() == SyntaxKind.ThisConstructorInitializer);
                    }
                }

                return primaryConstructor != null;
            }

            bool AnalyzeConstructorBody(
                INamedTypeSymbol namedType,
                ConstructorDeclarationSyntax primaryConstructorDeclaration,
                Dictionary<ISymbol, IParameterSymbol> candidateMembersToRemove)
            {
                var semanticModel = compilation.GetSemanticModel(primaryConstructorDeclaration.SyntaxTree);

                var body = primaryConstructorDeclaration.ExpressionBody ?? (SyntaxNode?)primaryConstructorDeclaration.Body;
                if (body?.ContainsDirectives is true)
                    return false;

                return primaryConstructorDeclaration switch
                {
                    { ExpressionBody.Expression: AssignmentExpressionSyntax assignmentExpression }
                        => IsAssignmentToInstanceMember(namedType, semanticModel, assignmentExpression, candidateMembersToRemove, orderedParameterAssignments: null, out _),
                    { Body: { } block }
                        => AnalyzeBlockBody(namedType, semanticModel, block, candidateMembersToRemove),
                    _ => false,
                };
            }

            bool AnalyzeBlockBody(
                INamedTypeSymbol namedType,
                SemanticModel semanticModel,
                BlockSyntax block,
                Dictionary<ISymbol, IParameterSymbol> candidateMembersToRemove)
            {
                // Quick pass.  Must all be assignment expressions.  Don't have to do any more analysis if we see anything beyond that.
                if (!block.Statements.All(static s => s is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax }))
                    return false;

                using var _1 = PooledHashSet<ISymbol>.GetInstance(out var assignedMembers);
                using var _2 = ArrayBuilder<(IParameterSymbol parameter, SyntaxNode assignedMemberDeclaration, bool parameterIsWrittenTo)>.GetInstance(out var orderedParameterAssignments);

                foreach (var statement in block.Statements)
                {
                    if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignmentExpression } ||
                        !IsAssignmentToInstanceMember(
                            namedType, semanticModel, assignmentExpression, candidateMembersToRemove, orderedParameterAssignments, out var member))
                    {
                        return false;
                    }

                    // Only allow a single write to the same member
                    if (!assignedMembers.Add(member))
                        return false;
                }

                // If we have a mutation of one of the parameters, and the parameter was referenced in multiple
                // assignments, then we can't convert this over if the order of actual members in the type is not the
                // same as the order of members we were assigning to.
                foreach (var group in orderedParameterAssignments.GroupBy(t => t.parameter))
                {
                    var parameter = group.Key;
                    if (group.Any(t => t.parameterIsWrittenTo) && group.Count() > 1)
                    {
                        var lastAssignedMemberDeclaration = group.First().assignedMemberDeclaration;
                        foreach (var (_, currentAssignedMemberDeclaration, _) in group.Skip(1))
                        {
                            // All the member decls have to be in the same containing type decl for this to be safe.
                            if (lastAssignedMemberDeclaration.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>() !=
                                currentAssignedMemberDeclaration.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>())
                            {
                                return false;
                            }

                            // They all have to be in order for this to be safe.
                            if (lastAssignedMemberDeclaration.SpanStart >= currentAssignedMemberDeclaration.SpanStart)
                                return false;

                            lastAssignedMemberDeclaration = currentAssignedMemberDeclaration;
                        }
                    }
                }

                return true;
            }

            bool IsAssignmentToInstanceMember(
                INamedTypeSymbol namedType,
                SemanticModel semanticModel,
                AssignmentExpressionSyntax assignmentExpression,
                Dictionary<ISymbol, IParameterSymbol> candidateMembersToRemove,
                ArrayBuilder<(IParameterSymbol parameter, SyntaxNode assignedMemberDeclaration, bool parameterIsWrittenTo)>? orderedParameterAssignments,
                [NotNullWhen(true)] out ISymbol? member)
            {
                member = null;

                if (assignmentExpression.Kind() != SyntaxKind.SimpleAssignmentExpression)
                    return false;

                // has to be of the form:
                //
                // x = ...      // or
                // this.x = ...
                var leftIdentifier = assignmentExpression.Left switch
                {
                    IdentifierNameSyntax identifierName => identifierName,
                    MemberAccessExpressionSyntax(kind: SyntaxKind.SimpleMemberAccessExpression) { Expression: (kind: SyntaxKind.ThisExpression), Name: IdentifierNameSyntax identifierName } => identifierName,
                    _ => null,
                };

                if (leftIdentifier is null)
                    return false;

                // Quick syntactic lookup.
                if (namedType.GetMembers(leftIdentifier.Identifier.ValueText).IsEmpty)
                    return false;

                // Has to bind to a field/prop on this type.
                member = semanticModel.GetSymbolInfo(leftIdentifier, cancellationToken).GetAnySymbol()?.OriginalDefinition;
                if (!IsViableMemberToAssignTo(namedType, member, out _, out var assignedMemberDeclaration, cancellationToken))
                    return false;

                // Left side looks good.  Now check the right side.  It cannot reference 'this' (as that is not
                // legal once we move this to initialize the field/prop in the rewrite).
                //
                // Note: we have to walk down suppressions as the IOp tree gives back nothing for them.
                var rightOperation = semanticModel.GetOperation(assignmentExpression.Right.WalkDownSuppressions());
                if (rightOperation is null)
                    return false;

                foreach (var operation in rightOperation.DescendantsAndSelf())
                {
                    if (operation is IInstanceReferenceOperation)
                        return false;

                    if (orderedParameterAssignments != null &&
                        operation is IParameterReferenceOperation { Syntax: IdentifierNameSyntax parameterName } parameterReference)
                    {
                        var isWrittenTo = parameterName.IsWrittenTo(semanticModel, cancellationToken);
                        orderedParameterAssignments.Add((parameterReference.Parameter, assignedMemberDeclaration, isWrittenTo));
                    }

                    // If we're referencing a local, then it must have been a local generated by an out-var, or a
                    // pattern.  And, if so, it's only safe to move this if the local we're referencing was produced
                    // under the same expression we're moving (not a prior statement).
                    if (operation is ILocalReferenceOperation { Local.DeclaringSyntaxReferences: [var syntaxRef, ..] } &&
                        !syntaxRef.GetSyntax(cancellationToken).AncestorsAndSelf().Any(a => a == assignmentExpression))
                    {
                        return false;
                    }
                }

                // Looks good, both the left and right sides are legal.

                // If we have an assignment of the form `private_member = param`, then that member can be a candidate for removal.
                if (member.DeclaredAccessibility == Accessibility.Private &&
                    !member.GetAttributes().Any() &&
                    semanticModel.GetSymbolInfo(assignmentExpression.Right, cancellationToken).GetAnySymbol() is IParameterSymbol parameter &&
                    parameter.Type.Equals(member.GetMemberType(), SymbolEqualityComparer.IncludeNullability))
                {
                    candidateMembersToRemove[member] = parameter;
                }

                return true;
            }
        }

        private void AnalyzeFieldOrPropertyReference(OperationAnalysisContext context)
        {
            var operation = (IMemberReferenceOperation)context.Operation;
            var semanticModel = operation.SemanticModel;
            Contract.ThrowIfNull(semanticModel);

            var instance = operation.Instance;

            // static field/property.  not something we're interested in.
            if (instance is null)
                return;

            var member = operation.Member.OriginalDefinition;

            // Don't need to analyze this again, if it's already in the list of members we can't remove.
            if (_membersThatCannotBeRemoved.Contains(member))
                return;

            // we only care about reference to members in our candidate-removal set.  Can ignore everything else.
            if (!_candidateMembersToRemove.TryGetValue(member, out var parameter))
                return;

            // not a reference off of 'this'.  e.g. `== other.fieldName`, we could not remove this member.
            if (instance is not IInstanceReferenceOperation)
            {
                _membersThatCannotBeRemoved.Add(member);
                return;
            }

            // it's either `this.field` or just `field`.  We can replace with a reference to 'paramName' *if* that
            // name in the current location doesn't bind to something else.
            var symbols = semanticModel.LookupSymbols(operation.Syntax.SpanStart, name: parameter.Name);
            if (symbols.Any(s => !Equals(s, parameter) && !Equals(s, member)))
            {
                _membersThatCannotBeRemoved.Add(member);
                return;
            }

            // looks good.  We should be able to remove this.
        }
    }
}
