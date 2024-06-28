// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;

using static SegmentedCollectionsMarshal;

/// <summary>
/// Looks for code of the form:
/// 
///     private ... object _gate = new object();
///     ...
///     lock (_gate)
///     {
///     }
///     
/// and converts it to:
/// 
///     private ... Lock _gate = new Lock();
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseSystemThreadingLockDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseSystemThreadingLockDiagnosticId,
        EnforceOnBuildValues.UseSystemThreadingLock,
        CSharpCodeStyleOptions.PreferSystemThreadingLock,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Use_System_Threading_Lock), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    /// <summary>
    /// A method body edit anywhere in a type will force us to reanalyze the whole type.
    /// </summary>
    /// <returns></returns>
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;

            // The new 'Lock' feature is only supported in C# 13 and above, and only if we actually have a definition of
            // System.Threading.Lock available.
            if (!compilation.LanguageVersion().IsCSharp13OrAbove())
                return;

            var lockType = compilation.GetTypeByMetadataName("System.Threading.Lock");
            if (lockType is not { DeclaredAccessibility: Accessibility.Public })
                return;

            context.RegisterSymbolStartAction(AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    private void AnalyzeNamedType(SymbolStartAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;

        var namedType = (INamedTypeSymbol)context.Symbol;
        if (namedType is not { TypeKind: TypeKind.Class or TypeKind.Struct })
            return;

        SyntaxTree? currentSyntaxTree = null;
        CodeStyleOption2<bool>? currentOption = null;

        // Needs to have a private field that is exactly typed as 'object'.  This way we can analyze all usages of it to
        // be sure it's completely safe to move to the new lock type.
        using var fieldsArray = TemporaryArray<(IFieldSymbol field, CodeStyleOption2<bool> option)>.Empty;
        using var _1 = PooledHashSet<SemanticModel>.GetInstance(out var cachedSemanticModels);

        foreach (var member in namedType.GetMembers())
        {
            if (member is not IFieldSymbol
                {
                    Type.SpecialType: SpecialType.System_Object,
                    DeclaredAccessibility: Accessibility.Private,
                    DeclaringSyntaxReferences: [var fieldSyntaxReference],
                } field)
            {
                continue;
            }

            var fieldSyntaxTree = fieldSyntaxReference.SyntaxTree;
            if (fieldSyntaxTree != currentSyntaxTree)
            {
                currentSyntaxTree = fieldSyntaxTree;

                currentOption = context.GetCSharpAnalyzerOptions(currentSyntaxTree).PreferSystemThreadingLock;

                // Ignore this field if it is is in a file that should be skipped.
                if (!currentOption.Value || ShouldSkipAnalysis(currentSyntaxTree, context.Options, context.Compilation.Options, currentOption.Notification, cancellationToken))
                    continue;
            }

            if (fieldSyntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax fieldSyntax)
                continue;

            // For simplicity, only offer this for fields with a single declarator.
            if (fieldSyntax.Parent is not VariableDeclarationSyntax { Parent: FieldDeclarationSyntax, Variables.Count: 1 })
                return;

            // If we have an initializer at the declaration site, it needs to be initialized with either `new object()`
            // or `new()`.
            if (fieldSyntax.Initializer != null && !IsSystemObjectCreationExpression(fieldSyntax.Initializer.Value))
                continue;

            // Looks like something that could be converted to a System.Threading.Lock if we see that this field is used
            // in a compatible fashion.
            fieldsArray.Add((field, currentOption!));
        }

        if (fieldsArray.Count == 0)
            return;

        // The set of fields we think could be converted to `System.Threading.Lock` from `object`.
        var potentialLockFields = new SegmentedDictionary<IFieldSymbol, (CodeStyleOption2<bool> option, bool canUse)>();

        // Whether or not we saw this field used in a `lock (obj)` statement.  If not, we do not want to convert this as
        // the user wasn't using this as a lock.  Note: we can consider expanding the set of patterns we detect (like
        // Monitor.Enter + Monitor.Exit) if we think it's worthwhile.
        var wasLockedSet = new ConcurrentSet<IFieldSymbol>();
        foreach (var (field, option) in fieldsArray)
            potentialLockFields[field] = (option, canUse: true);

        // Now go see how the code within this named type actually uses any fields within.
        context.RegisterOperationAction(context =>
        {
            var cancellationToken = context.CancellationToken;
            var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;
            var fieldReference = fieldReferenceOperation.Field.OriginalDefinition;

            // We only care about examining field references to the fields we're considering converting to System.Threading.Lock.
            if (!potentialLockFields.ContainsKey(fieldReference))
                return;

            if (fieldReferenceOperation.Parent is ILockOperation lockOperation)
            {
                // Locking on the the new lock type disallows yielding inside the lock.  So if we see that, immediately
                // consider this not applicable.
                if (lockOperation.Syntax.ContainsYield())
                {
                    GetValueRefOrNullRef(potentialLockFields, fieldReference).canUse = false;
                    return;
                }

                // We did lock on this field, mark as such as its now something we'd def like to convert to a
                // System.Threading.Lock if possible.
                wasLockedSet.Add(fieldReference);
                return;
            }

            // It's ok to assign to the field, as long as we're assigning a new lock object to it. e.g.  `_gate = new
            // object()` is fine to continue converting over to System.Threading.Lock.  But an assignment of something
            // else is not.
            if (fieldReferenceOperation.Parent is IAssignmentOperation { Syntax: AssignmentExpressionSyntax assignmentSyntax } assignment &&
                assignment.Target == fieldReferenceOperation &&
                IsSystemObjectCreationExpression(assignmentSyntax.Right))
            {
                var operand = assignment.Value is IConversionOperation { Conversion: { Exists: true, IsImplicit: true } } conversion
                    ? conversion.Operand
                    : assignment.Value;

                if (operand is IObjectCreationOperation { Arguments.Length: 0, Constructor.ContainingType.SpecialType: SpecialType.System_Object })
                    return;
            }

            // Fine to use `nameof(someLock)` as that's not actually using the lock.
            if (fieldReferenceOperation.Parent is INameOfOperation)
                return;

            // Note: More patterns can be added here as needed.

            // This wasn't a supported case.  Immediately disallow conversion of this field.
            GetValueRefOrNullRef(potentialLockFields, fieldReference).canUse = false;
        }, OperationKind.FieldReference);

        context.RegisterSymbolEndAction(context =>
        {
            var cancellationToken = context.CancellationToken;

            foreach (var (lockField, (option, canUse)) in potentialLockFields)
            {
                // If we blocked this field in our analysis pass, can immediately skip.
                if (!canUse)
                    continue;

                // Has to at least see this field locked on to offer to convert it to a Lock.
                if (!wasLockedSet.Contains(lockField))
                    continue;

                // .Single is safe here as we confirmed there was only one DeclaringSyntaxReference in the field at the beginning of analysis.
                var declarator = (VariableDeclaratorSyntax)lockField.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken);

                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    declarator.Identifier.GetLocation(),
                    option.Notification,
                    context.Options,
                    additionalLocations: null,
                    properties: null));
            }
        });

        static bool IsSystemObjectCreationExpression(ExpressionSyntax expression)
        {
            // new()
            if (expression is ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 })
                return true;

            // new ...()
            if (expression is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } objectCreationExpression)
            {
                // new object();
                if (objectCreationExpression.Type is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.ObjectKeyword })
                    return true;

                // new Object(), new System.Object(), etc. Almost certain the right type as it would be pathological to
                // actually have a user type named Object (especially used as a lock).
                if (objectCreationExpression.Type is IdentifierNameSyntax { Identifier.ValueText: nameof(System.Object) })
                    return true;

                if (objectCreationExpression.Type is QualifiedNameSyntax { Right.Identifier.ValueText: nameof(System.Object) })
                    return true;
            }

            return false;
        }
    }
}
