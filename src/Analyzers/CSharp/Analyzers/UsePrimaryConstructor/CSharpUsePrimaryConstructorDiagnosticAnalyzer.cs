// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: kvp

using System;
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

                context.RegisterSymbolStartAction(context => Analyzer.AnalyzeNamedTypeStart(this, context), SymbolKind.NamedType);
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
                PooledDictionary<ISymbol, IParameterSymbol> candidateMembersToRemove)
            {
                _diagnosticAnalyzer = diagnosticAnalyzer;
                _styleOption = styleOption;
                _primaryConstructor = primaryConstructor;
                _primaryConstructorDeclaration = primaryConstructorDeclaration;
                _candidateMembersToRemove = candidateMembersToRemove;
            }

            private void OnSymbolEnd(SymbolAnalysisContext context)
            {
                // See if the constructor analysis found a viable constructor to convert to a primary constructor.
                if (_primaryConstructor is not null)
                {
                    // Pass along a mapping of field/property name to the constructor parameter name that will replace it.
                    var properties = _candidateMembersToRemove
                        .Where(kvp => !_membersThatCannotBeRemoved.Contains(kvp.Key))
                        .ToImmutableDictionary(static kvp => kvp.Key.Name, static kvp => (string?)kvp.Value.Name);

                    if (_candidateMembersToRemove.All(kvp => kvp.Key is IFieldSymbol))
                    {
                        properties = properties.Add(AllFieldsName, AllFieldsName);
                    }
                    else if (_candidateMembersToRemove.All(kvp => kvp.Key is IPropertySymbol))
                    {
                        properties = properties.Add(AllPropertiesName, AllPropertiesName);
                    }

                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        _diagnosticAnalyzer.Descriptor,
                        _primaryConstructorDeclaration.Identifier.GetLocation(),
                        _styleOption.Notification.Severity,
                        ImmutableArray.Create(_primaryConstructorDeclaration.GetLocation()),
                        properties));
                }

                _candidateMembersToRemove.Free();
                s_concurrentSetPool.ClearAndFree(_membersThatCannotBeRemoved);
            }

            public static void AnalyzeNamedTypeStart(
                CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer,
                SymbolStartAnalysisContext context)
            {
                var compilation = context.Compilation;
                var cancellationToken = context.CancellationToken;

                // Bail immediately if the user has disabled this feature.
                var namedType = (INamedTypeSymbol)context.Symbol;
                if (namedType.DeclaringSyntaxReferences is not [var reference, ..])
                    return;

                var styleOption = context.Options.GetCSharpAnalyzerOptions(reference.SyntaxTree).PreferPrimaryConstructors;
                if (!styleOption.Value)
                    return;

                // only classes/structs can have primary constructors (not interfaces, enums or delegates).
                if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                    return;

                // Don't want to offer on records.  It's completely fine for records to not use primary constructs and
                // instead use init-properties.
                if (namedType.IsRecord)
                    return;

                // No need to offer this if the type already has a primary constructor.
                if (namedType.TryGetPrimaryConstructor(out _))
                    return;

                // Need to see if there is a single constructor that either calls `base(...)` or has no constructor
                // initializer (and thus implicitly calls `base()`), and that all other constructors call `this(...)`.
                if (!TryFindPrimaryConstructorCandidate(out var primaryConstructor, out var primaryConstructorDeclaration))
                    return;

                if (primaryConstructor.Parameters.Length == 0)
                    return;

                if (primaryConstructor.DeclaredAccessibility != Accessibility.Public)
                    return;

                // Constructor has to have a real body (can't be extern/etc.).
                if (primaryConstructorDeclaration is { Body: null, ExpressionBody: null })
                    return;

                if (primaryConstructorDeclaration.Parent is not TypeDeclarationSyntax)
                    return;

                // Now ensure the constructor body is something that could be converted to a primary constructor (i.e.
                // only assignments to instance fields/props on this).
                var semanticModel = compilation.GetSemanticModel(primaryConstructorDeclaration.SyntaxTree);
                var candidateMembersToRemove = PooledDictionary<ISymbol, IParameterSymbol>.GetInstance();
                if (!AnalyzeConstructorBody())
                {
                    candidateMembersToRemove.Free();
                    return;
                }

                var analyzer = new Analyzer(diagnosticAnalyzer, styleOption, primaryConstructor, primaryConstructorDeclaration, candidateMembersToRemove);
                context.RegisterSymbolEndAction(analyzer.OnSymbolEnd);

                // Look to see if we have trivial `_x = x` or `this.x = x` assignments.  If so, then the field/prop
                // could be a candidate for removal (as long as we determine that all use sites of the field/prop would
                // be able to use the captured primary constructor parameter.

                if (candidateMembersToRemove.Count > 0)
                    context.RegisterOperationAction(analyzer.AnalyzeFieldOrPropertyReference, OperationKind.FieldReference, OperationKind.PropertyReference);

                return;

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

                bool AnalyzeConstructorBody()
                {
                    return primaryConstructorDeclaration switch
                    {
                        { ExpressionBody.Expression: AssignmentExpressionSyntax assignmentExpression } => IsAssignmentToInstanceMember(assignmentExpression, out _),
                        { Body: { } body } => AnalyzeBlockBody(body),
                        _ => false,
                    };
                }

                bool AnalyzeBlockBody(BlockSyntax block)
                {
                    // Quick pass.  Must all be assignment expressions.  Don't have to do any more analysis if we see anything beyond that.
                    if (!block.Statements.All(static s => s is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax }))
                        return false;

                    using var _ = PooledHashSet<ISymbol>.GetInstance(out var assignedMembers);

                    foreach (var statement in block.Statements)
                    {
                        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignmentExpression } ||
                            !IsAssignmentToInstanceMember(assignmentExpression, out var member))
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
                    AssignmentExpressionSyntax assignmentExpression, [NotNullWhen(true)] out ISymbol? member)
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
                        semanticModel.GetSymbolInfo(assignmentExpression.Right, cancellationToken).GetAnySymbol() is IParameterSymbol parameter)
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

                var member = operation.Member.OriginalDefinition;
                var instance = operation.Instance;

                // static field/property.  not something we're interested in.
                if (instance is null)
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
}
