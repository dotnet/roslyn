// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
                if (context.Compilation.LanguageVersion().IsCSharp12OrAbove())
                    return;

                var analyzer = new Analyzer(this);
                context.RegisterSymbolStartAction(analyzer.AnalyzeNamedTypeStart, SymbolKind.NamedType);
            });
        }

        private sealed class Analyzer
        {
            private readonly CSharpUsePrimaryConstructorDiagnosticAnalyzer _diagnosticAnalyzer;

            private INamedTypeSymbol _namedType = null!;
            private CodeStyleOption2<bool> _styleOption = null!;

            private IMethodSymbol _primaryConstructor = null!;
            private ConstructorDeclarationSyntax _primaryConstructorDeclaration = null!;

            private readonly PooledDictionary<ISymbol, IParameterSymbol> _candidateMembersToRemove = PooledDictionary<ISymbol, IParameterSymbol>.GetInstance();
            private readonly PooledHashSet<ISymbol> _membersThatCannotBeRemoved = PooledHashSet<ISymbol>.GetInstance();

            public Analyzer(CSharpUsePrimaryConstructorDiagnosticAnalyzer diagnosticAnalyzer)
            {
                _diagnosticAnalyzer = diagnosticAnalyzer;
            }

            private void OnSymbolEnd(SymbolAnalysisContext context)
            {
                // See if the constructor analysis found a viable constructor to convert to a primary constructor.
                if (_primaryConstructor is not null)
                {
                    // Pass along a mapping of field/property name to the constructor parameter name that will replace it.
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        _diagnosticAnalyzer.Descriptor,
                        _primaryConstructorDeclaration.Identifier.GetLocation(),
                        _styleOption.Notification.Severity,
                        ImmutableArray.Create(_primaryConstructorDeclaration.GetLocation()),
                        _candidateMembersToRemove
                            .Where(kvp => !_membersThatCannotBeRemoved.Contains(kvp.Key))
                            .ToImmutableDictionary(static kvp => kvp.Key.Name, static kvp => (string?)kvp.Value.Name)));
                }

                _candidateMembersToRemove.Free();
                _membersThatCannotBeRemoved.Free();
            }

            public void AnalyzeNamedTypeStart(SymbolStartAnalysisContext context)
            {
                context.RegisterSymbolEndAction(OnSymbolEnd);

                var cancellationToken = context.CancellationToken;

                // Bail immediately if the user has disabled this feature.
                _namedType = (INamedTypeSymbol)context.Symbol;
                if (_namedType.DeclaringSyntaxReferences is not [var reference, ..])
                    return;

                _styleOption = context.Options.GetCSharpAnalyzerOptions(reference.SyntaxTree).PreferPrimaryConstructors;
                if (!_styleOption.Value)
                    return;

                // only classes/structs can have primary constructors (not interfaces, enums or delegates).
                if (_namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                    return;

                // Don't want to offer on records.  It's completely fine for records to not use primary constructs and
                // instead use init-properties.
                if (_namedType.IsRecord)
                    return;

                // No need to offer this if the type already has a primary constructor.
                if (_namedType.TryGetPrimaryConstructor(out _))
                    return;

                // Need to see if there is a single constructor that either calls `base(...)` or has no constructor
                // initializer (and thus implicitly calls `base()`), and that all other constructors call `this(...)`.
                if (!TryFindPrimaryConstructorCandidate())
                    return;

                Contract.ThrowIfNull(_primaryConstructor);
                Contract.ThrowIfNull(_primaryConstructorDeclaration);

                // Constructor has to have a real body (can't be extern/etc.).
                if (_primaryConstructorDeclaration is { Body: null, ExpressionBody: null })
                    return;

                if (_primaryConstructorDeclaration.Parent is not TypeDeclarationSyntax)
                    return;

                // Now ensure the constructor body is something that could be converted to a primary constructor (i.e.
                // only assignments to instance fields/props on this).
                var semanticModel = context.Compilation.GetSemanticModel(_primaryConstructorDeclaration.SyntaxTree);
                if (!AnalyzeConstructorBody())
                {
                    // Anything else is invalid.  Clear this out so that OnSymbolEnd knows to do nothing.
                    _primaryConstructor = null!;
                    _primaryConstructorDeclaration = null!;

                    return;
                }

                // Look to see if we have trivial `_x = x` or `this.x = x` assignments.  If so, then the field/prop
                // could be a candidate for removal (as long as we determine that all use sites of the field/prop would
                // be able to use the captured primary constructor parameter.

                if (_candidateMembersToRemove.Count > 0)
                    context.RegisterOperationAction(AnalyzeFieldOrPropertyReference, OperationKind.FieldReference, OperationKind.PropertyReference);

                return;

                bool TryFindPrimaryConstructorCandidate()
                {
                    var constructors = _namedType.InstanceConstructors;

                    foreach (var constructor in constructors)
                    {
                        // Needs to just have a single declaration
                        if (constructor.DeclaringSyntaxReferences is not [var constructorReference])
                            return false;

                        if (constructorReference.GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
                            return false;

                        if (constructorDeclaration.Initializer is null or (kind: SyntaxKind.BaseConstructorInitializer))
                        {
                            // Can only have one candidate
                            if (_primaryConstructor != null)
                                return false;

                            _primaryConstructor = constructor;
                            _primaryConstructorDeclaration = constructorDeclaration;
                        }
                        else
                        {
                            Debug.Assert(constructorDeclaration.Initializer.Kind() == SyntaxKind.ThisConstructorInitializer);
                        }
                    }

                    return _primaryConstructor != null;
                }

                bool AnalyzeConstructorBody()
                {
                    return _primaryConstructorDeclaration switch
                    {
                        { ExpressionBody.Expression: AssignmentExpressionSyntax assignmentExpression } => IsAssignmentToInstanceMember(assignmentExpression),
                        { Body: { } body } => AnalyzeBlockBody(body),
                        _ => false,
                    };
                }

                bool AnalyzeBlockBody(BlockSyntax block)
                {
                    // Quick pass.  Must all be assignment expressions.  Don't have to do any more analysis if we see anything beyond that.
                    if (!block.Statements.All(static s => s is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax }))
                        return false;

                    foreach (var statement in block.Statements)
                    {
                        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignmentExpression } ||
                            !IsAssignmentToInstanceMember(assignmentExpression))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                bool IsAssignmentToInstanceMember(AssignmentExpressionSyntax assignmentExpression)
                {
                    // has to be of the form:
                    //
                    // x = ...      // or
                    // this.x = ...
                    var leftIdentifier = assignmentExpression.Left switch
                    {
                        IdentifierNameSyntax identifierName => identifierName,
                        BinaryExpressionSyntax(kind: SyntaxKind.SimpleMemberAccessExpression) { Left: (kind: SyntaxKind.ThisExpression), Right: IdentifierNameSyntax identifierName } => identifierName,
                        _ => null,
                    };

                    if (leftIdentifier is null)
                        return false;

                    // Quick syntactic lookup.
                    if (_namedType.GetMembers(leftIdentifier.Identifier.ValueText).IsEmpty)
                        return false;

                    // Has to bind to a field/prop on this type.
                    var leftSymbol = semanticModel.GetSymbolInfo(leftIdentifier, cancellationToken).GetAnySymbol();
                    if (leftSymbol is not IFieldSymbol and not IPropertySymbol)
                        return false;

                    if (leftSymbol.IsStatic)
                        return false;

                    if (!_namedType.Equals(leftSymbol.ContainingType))
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
                    if (leftSymbol.DeclaredAccessibility == Accessibility.Private &&
                        semanticModel.GetSymbolInfo(assignmentExpression.Right, cancellationToken).GetAnySymbol() is IParameterSymbol parameter)
                    {
                        _candidateMembersToRemove[leftSymbol.OriginalDefinition] = parameter;
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
                if (symbols.Length > 0)
                {
                    _membersThatCannotBeRemoved.Add(member);
                    return;
                }

                // looks good.  We should be able to remove this.
            }
        }
    }
}
