// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: kvp

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor
{

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
    internal sealed class CSharpUsePrimaryConstructorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        // Deliberately using names that could not be actual field/property names in the properties dictionary.
        public const string AllFieldsName = "<>AllFields";
        public const string AllPropertiesName = "<>AllProperties";

        private static readonly ObjectPool<ConcurrentSet<ISymbol>> s_concurrentSetPool = new(() => new());

        public CSharpUsePrimaryConstructorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId,
                   EnforceOnBuildValues.UsePrimaryConstructor,
                   CSharpCodeStyleOptions.PreferPrimaryConstructors,
                   new LocalizableResourceString(
                        nameof(CSharpAnalyzersResources.Use_primary_constructor), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                // "x is not Type y" is only available in C# 9.0 and above. Don't offer this refactoring
                // in projects targeting a lesser version.
                if (!context.Compilation.LanguageVersion().IsCSharp12OrAbove())
                    return;

                // Mapping from a named type to a particular analyzer we have created for it. Needed because nested
                // types need to update the information for their containing types while they themselves are being
                // analyzed.  Specifically, we need to look for 
                var namedTypeToAnalyzer = new ConcurrentDictionary<INamedTypeSymbol, Analyzer>();
                context.RegisterSymbolStartAction(context => Analyzer.AnalyzeNamedTypeStart(this, context, namedTypeToAnalyzer), SymbolKind.NamedType);
            });
        }

        public static bool IsViableMemberToAssignTo(
            INamedTypeSymbol namedType,
            [NotNullWhen(true)] ISymbol? member,
            [NotNullWhen(true)] out SyntaxNode? nodeToRemove,
            CancellationToken cancellationToken)
        {
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
            }

            return true;
        }

        /// <summary>
        /// Helper type we create that encapsulates all the state we need while processing.
        /// </summary>
        private sealed class Analyzer
        {
            private readonly CSharpUsePrimaryConstructorDiagnosticAnalyzer _diagnosticAnalyzer;

            private readonly CodeStyleOption2<bool> _styleOption;

            private readonly IMethodSymbol _primaryConstructor;
            private readonly ConstructorDeclarationSyntax _primaryConstructorDeclaration;

            private readonly PooledDictionary<ISymbol, IParameterSymbol> _candidateMembersToRemove;
            private readonly ConcurrentDictionary<INamedTypeSymbol, Analyzer> _namedTypeToAnalyzer;

            /// <summary>
            /// Needs to be concurrent as we can process members in parallel in <see
            /// cref="AnalyzeFieldOrPropertyReference"/>.
            /// </summary>
            private readonly ConcurrentSet<ISymbol> _membersThatCannotBeRemoved = s_concurrentSetPool.Allocate();

            public Analyzer(
                CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer,
                CodeStyleOption2<bool> styleOption,
                IMethodSymbol primaryConstructor,
                ConstructorDeclarationSyntax primaryConstructorDeclaration,
                PooledDictionary<ISymbol, IParameterSymbol> candidateMembersToRemove,
                ConcurrentDictionary<INamedTypeSymbol, Analyzer> namedTypeToAnalyzer)
            {
                _diagnosticAnalyzer = diagnosticAnalyzer;
                _styleOption = styleOption;
                _primaryConstructor = primaryConstructor;
                _primaryConstructorDeclaration = primaryConstructorDeclaration;
                _candidateMembersToRemove = candidateMembersToRemove;
                _namedTypeToAnalyzer = namedTypeToAnalyzer;

                namedTypeToAnalyzer.TryAdd(primaryConstructor.ContainingType, this);
            }

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
                    _styleOption.Notification.Severity,
                    ImmutableArray.Create(_primaryConstructorDeclaration.GetLocation()),
                    properties));

                _candidateMembersToRemove.Free();
                s_concurrentSetPool.ClearAndFree(_membersThatCannotBeRemoved);

                _namedTypeToAnalyzer.TryRemove(_primaryConstructor.ContainingType, out _);
            }

            public static void AnalyzeNamedTypeStart(
                CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer,
                SymbolStartAnalysisContext context,
                ConcurrentDictionary<INamedTypeSymbol, Analyzer> namedTypeToAnalyzer)
            {
                var compilation = context.Compilation;
                var cancellationToken = context.CancellationToken;

                // Bail immediately if the user has disabled this feature.
                var namedType = (INamedTypeSymbol)context.Symbol;
                var options = context.Options;

                if (namedType.Name == "OuterType")
                    Thread.Sleep(2000);

                var candidateMembersToRemove = PooledDictionary<ISymbol, IParameterSymbol>.GetInstance();
                if (!TryCreateAnalyzer(out var analyzer))
                {
                    candidateMembersToRemove.Free();

                    // We're not analyzing this type itself.  But we still need to hear about field/property references
                    // within it if it's a nested type and one of its containers is a type we're analyzing.
                    for (var currentType = namedType.ContainingType; currentType != null; currentType = currentType.ContainingType)
                    {
                        if (namedTypeToAnalyzer.TryGetValue(currentType, out var containingTypeAnalyzer))
                            RegisterFieldOrPropertyAnalysisIfNecessary(containingTypeAnalyzer);
                    }
                }
                else
                {
                    // Look to see if we have trivial `_x = x` or `this.x = x` assignments.  If so, then the field/prop
                    // could be a candidate for removal (as long as we determine that all use sites of the field/prop would
                    // be able to use the captured primary constructor parameter).
                    RegisterFieldOrPropertyAnalysisIfNecessary(analyzer);

                    context.RegisterSymbolEndAction(analyzer.OnSymbolEnd);
                }

                return;

                void RegisterFieldOrPropertyAnalysisIfNecessary(Analyzer analyzer)
                {
                    if (analyzer.HasCandidateMembersToRemove)
                    {
                        context.RegisterOperationAction(
                            context => AnalyzeFieldOrPropertyReference(context, namedTypeToAnalyzer),
                            OperationKind.FieldReference, OperationKind.PropertyReference);
                    }
                }

                bool TryCreateAnalyzer([NotNullWhen(true)] out Analyzer? analyzer)
                {
                    analyzer = null;

                    // Bail immediately if the user has disabled this feature.
                    if (namedType.DeclaringSyntaxReferences is not [var reference, ..])
                        return false;

                    var styleOption = options.GetCSharpAnalyzerOptions(reference.SyntaxTree).PreferPrimaryConstructors;
                    if (!styleOption.Value)
                        return false;

                    // only classes/structs can have primary constructors (not interfaces, enums or delegates).
                    if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                        return false;

                    // Don't want to offer on records.  It's completely fine for records to not use primary constructs and
                    // instead use init-properties.
                    if (namedType.IsRecord)
                        return false;

                    // No need to offer this if the type already has a primary constructor.
                    if (namedType.TryGetPrimaryConstructor(out _))
                        return false;

                    // Need to see if there is a single constructor that either calls `base(...)` or has no constructor
                    // initializer (and thus implicitly calls `base()`), and that all other constructors call `this(...)`.
                    if (!TryFindPrimaryConstructorCandidate(out var primaryConstructor, out var primaryConstructorDeclaration))
                        return false;

                    if (primaryConstructor.Parameters.Length == 0)
                        return false;

                    if (primaryConstructor.DeclaredAccessibility != Accessibility.Public)
                        return false;

                    // Constructor has to have a real body (can't be extern/etc.).
                    if (primaryConstructorDeclaration is { Body: null, ExpressionBody: null })
                        return false;

                    if (primaryConstructorDeclaration.Parent is not TypeDeclarationSyntax)
                        return false;

                    if (primaryConstructor.Parameters.Any(static p => p.RefKind is RefKind.Ref or RefKind.Out))
                        return false;

                    // Now ensure the constructor body is something that could be converted to a primary constructor (i.e.
                    // only assignments to instance fields/props on this).
                    if (!AnalyzeConstructorBody(primaryConstructorDeclaration))
                        return false;

                    analyzer = new Analyzer(diagnosticAnalyzer, styleOption, primaryConstructor, primaryConstructorDeclaration, candidateMembersToRemove, namedTypeToAnalyzer);
                    return true;
                }

                bool TryFindPrimaryConstructorCandidate(
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

                bool AnalyzeConstructorBody(ConstructorDeclarationSyntax primaryConstructorDeclaration)
                {
                    var semanticModel = compilation.GetSemanticModel(primaryConstructorDeclaration.SyntaxTree);
                    return primaryConstructorDeclaration switch
                    {
                        { ExpressionBody.Expression: AssignmentExpressionSyntax assignmentExpression } => IsAssignmentToInstanceMember(semanticModel, assignmentExpression, out _),
                        { Body: { } body } => AnalyzeBlockBody(semanticModel, body),
                        _ => false,
                    };
                }

                bool AnalyzeBlockBody(SemanticModel semanticModel, BlockSyntax block)
                {
                    // Quick pass.  Must all be assignment expressions.  Don't have to do any more analysis if we see anything beyond that.
                    if (!block.Statements.All(static s => s is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax }))
                        return false;

                    using var _ = PooledHashSet<ISymbol>.GetInstance(out var assignedMembers);

                    foreach (var statement in block.Statements)
                    {
                        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignmentExpression } ||
                            !IsAssignmentToInstanceMember(semanticModel, assignmentExpression, out var member))
                        {
                            return false;
                        }

                        // Only allow a single write to the same member
                        if (!assignedMembers.Add(member))
                            return false;
                    }

                    return true;
                }

                bool IsAssignmentToInstanceMember(
                    SemanticModel semanticModel, AssignmentExpressionSyntax assignmentExpression, [NotNullWhen(true)] out ISymbol? member)
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
                    if (!IsViableMemberToAssignTo(namedType, member, out _, cancellationToken))
                        return false;

                    // Left side looks good.  Now check the right side.  It cannot reference 'this' (as that is not
                    // legal once we move this to initialize the field/prop in the rewrite).
                    var rightOperation = semanticModel.GetOperation(assignmentExpression.Right);
                    foreach (var operation in rightOperation.DescendantsAndSelf())
                    {
                        if (operation is IInstanceReferenceOperation)
                            return false;
                    }

                    // Looks good, both the left and right sides are legal.

                    // If we have an assignment of the form `private_member = param`, then that member can be a candidate for removal.
                    if (member.DeclaredAccessibility == Accessibility.Private &&
                        !member.GetAttributes().Any() &&
                        semanticModel.GetSymbolInfo(assignmentExpression.Right, cancellationToken).GetAnySymbol() is IParameterSymbol parameter &&
                        parameter.Type.Equals(member.GetMemberType()))
                    {
                        candidateMembersToRemove[member] = parameter;
                    }

                    return true;
                }
            }

            private static void AnalyzeFieldOrPropertyReference(OperationAnalysisContext context, ConcurrentDictionary<INamedTypeSymbol, Analyzer> namedTypeToAnalyzer)
            {
                var operation = (IMemberReferenceOperation)context.Operation;
                var semanticModel = operation.SemanticModel;
                Contract.ThrowIfNull(semanticModel);

                var instance = operation.Instance;

                // static field/property.  not something we're interested in.
                if (instance is null)
                    return;

                var member = operation.Member.OriginalDefinition;
                var namedType = member.ContainingType.OriginalDefinition;

                // See if this is field/prop access on a named type that we're analyzing.
                if (!namedTypeToAnalyzer.TryGetValue(namedType, out var analyzer))
                    return;

                // Don't need to analyze this again, if it's already in the list of members we can't remove.
                if (analyzer._membersThatCannotBeRemoved.Contains(member))
                    return;

                // we only care about reference to members in our candidate-removal set.  Can ignore everything else.
                if (!analyzer._candidateMembersToRemove.TryGetValue(member, out var parameter))
                    return;

                // not a reference off of 'this'.  e.g. `== other.fieldName`, we could not remove this member.
                if (instance is not IInstanceReferenceOperation)
                {
                    analyzer._membersThatCannotBeRemoved.Add(member);
                    return;
                }

                // it's either `this.field` or just `field`.  We can replace with a reference to 'paramName' *if* that
                // name in the current location doesn't bind to something else.
                var symbols = semanticModel.LookupSymbols(operation.Syntax.SpanStart, name: parameter.Name);
                if (symbols.Any(s => !Equals(s, parameter) && !Equals(s, member)))
                {
                    analyzer._membersThatCannotBeRemoved.Add(member);
                    return;
                }

                // looks good.  We should be able to remove this.
            }
        }
    }
}
