// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;

using static SegmentedCollectionsMarshal;
using PotentialLockFields = SegmentedDictionary<IFieldSymbol, (VariableDeclaratorSyntax declarator, CodeStyleOption2<bool> option, bool canUse, bool wasLocked)>;

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

            var lockType = compilation.GetBestTypeByMetadataName("System.Threading.Lock");
            if (lockType is not { DeclaredAccessibility: Accessibility.Public })
                return;

            if (lockType.GetTypeMembers("Scope").FirstOrDefault() is not { TypeKind: TypeKind.Struct, IsRefLikeType: true, DeclaredAccessibility: Accessibility.Public })
                return;

            // Private fields of a type can be referenced from its nested types, which are analyzed separately.  This
            // maps a type that has nested types to the fields it could convert, so that its nested types can report
            // how they use those fields.
            var namedTypeToPotentialLockFields = new ConcurrentDictionary<INamedTypeSymbol, PotentialLockFields?>();
            context.RegisterSymbolStartAction(
                context => AnalyzeNamedType(context, namedTypeToPotentialLockFields), SymbolKind.NamedType);
        });
    }

    private void AnalyzeNamedType(
        SymbolStartAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, PotentialLockFields?> namedTypeToPotentialLockFields)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // This type can reference the private fields of any type that contains it, so check those references too.  The
        // containing type's symbol end action only runs once its nested types have been analyzed.
        for (var containingType = namedType.ContainingType; containingType != null; containingType = containingType.ContainingType)
        {
            var containingTypeLockFields = namedTypeToPotentialLockFields.GetOrAdd(
                containingType, containingType => GetPotentialLockFields(context, containingType));
            if (containingTypeLockFields != null)
                context.RegisterOperationAction(context => AnalyzeFieldReference(context, containingTypeLockFields), OperationKind.FieldReference);
        }

        var potentialLockFields = namedType.GetTypeMembers().IsEmpty
            ? GetPotentialLockFields(context, namedType)
            : namedTypeToPotentialLockFields.GetOrAdd(namedType, namedType => GetPotentialLockFields(context, namedType));
        if (potentialLockFields is null)
            return;

        // Register a callback to ensure the field's initializer is either missing, or is only instantiating the field
        // with a new object.
        context.RegisterOperationAction(context =>
        {
            if (context.ContainingSymbol is not IFieldSymbol fieldSymbol)
                return;

            ref var valueRef = ref GetValueRefOrNullRef(potentialLockFields, fieldSymbol);
            if (Unsafe.IsNullRef(ref valueRef))
                return;

            var fieldInitializer = (IFieldInitializerOperation)context.Operation;
            if (fieldInitializer.Value is null)
                return;

            if (!IsObjectCreationOperation(fieldInitializer.Value))
                valueRef.canUse = false;
        }, OperationKind.FieldInitializer);

        // Now go see how the code within this named type actually uses any fields within.
        context.RegisterOperationAction(context => AnalyzeFieldReference(context, potentialLockFields), OperationKind.FieldReference);

        // Finally, once we're done analyzing the symbol body, report diagnostics for any fields that we determined are
        // locks and can be safely converted.
        context.RegisterSymbolEndAction(context =>
        {
            namedTypeToPotentialLockFields.TryRemove(namedType, out _);

            foreach (var (lockField, (declarator, option, canUse, wasLocked)) in potentialLockFields)
            {
                // If we blocked this field in our analysis pass, can immediately skip.
                if (!canUse)
                    continue;

                // Has to at least see this field locked on to offer to convert it to a Lock.
                if (!wasLocked)
                    continue;

                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    declarator.Identifier.GetLocation(),
                    option.Notification,
                    context.Options,
                    additionalLocations: null,
                    properties: null));
            }
        });
    }

    /// <summary>
    /// Returns the fields of <paramref name="namedType"/> that could be converted to <c>System.Threading.Lock</c> if
    /// they turn out to be used compatibly, or <see langword="null"/> if there are none.
    /// </summary>
    private PotentialLockFields? GetPotentialLockFields(SymbolStartAnalysisContext context, INamedTypeSymbol namedType)
    {
        var cancellationToken = context.CancellationToken;

        if (namedType is not { TypeKind: TypeKind.Class or TypeKind.Struct })
            return null;

        SyntaxTree? currentSyntaxTree = null;
        CodeStyleOption2<bool>? currentOption = null;
        var skipCurrentSyntaxTree = false;

        // Needs to have a private field that is exactly typed as 'object'.  This way we can analyze all usages of it to
        // be sure it's completely safe to move to the new lock type.
        using var fieldsArray = TemporaryArray<(IFieldSymbol field, VariableDeclaratorSyntax declarator, CodeStyleOption2<bool> option)>.Empty;

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

            // The option is only read for the first field of each syntax tree, so remember whether that tree is skipped
            // and apply it to every field in it.
            var fieldSyntaxTree = fieldSyntaxReference.SyntaxTree;
            if (fieldSyntaxTree != currentSyntaxTree)
            {
                currentSyntaxTree = fieldSyntaxTree;
                currentOption = context.GetCSharpAnalyzerOptions(currentSyntaxTree).PreferSystemThreadingLock;
                skipCurrentSyntaxTree = !currentOption.Value || ShouldSkipAnalysis(currentSyntaxTree, context.Options, context.Compilation.Options, currentOption.Notification, cancellationToken);
            }

            // Ignore this field if it is is in a file that should be skipped.
            if (skipCurrentSyntaxTree)
                continue;

            if (fieldSyntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax variableDeclarator)
                continue;

            // For simplicity, only offer this for fields with a single declarator.
            if (variableDeclarator.Parent is not VariableDeclarationSyntax { Parent: FieldDeclarationSyntax, Variables.Count: 1 })
                continue;

            // Looks like something that could be converted to a System.Threading.Lock if we see that this field is used
            // in a compatible fashion.
            fieldsArray.Add((field, variableDeclarator, currentOption!));
        }

        if (fieldsArray.Count == 0)
            return null;

        // The set of fields we think could be converted to `System.Threading.Lock` from `object`.
        //
        // 'wasLocked' tracks whether or not we saw this field used in a `lock (obj)` statement.  If not, we do not want
        // to convert this as the user wasn't using this as a lock.  Note: we can consider expanding the set of patterns
        // we detect (like Monitor.Enter + Monitor.Exit) if we think it's worthwhile.
        //
        // Note: both this dictionary is written to concurrently in the analysis pass below.  This is safe as we never
        // are actually mutating the dictionary itself (we're not adding/removing items).  We're just getting a
        // reference to its tuple value and overwriting a bool in place within that tuple with the new value.  And we
        // only move hte value in one direction.  For example 'canUse' only moved from 'true' to 'false' and 'wasLocked'
        // only moves the value from 'false' to 'true'.
        var potentialLockFields = new PotentialLockFields(capacity: fieldsArray.Count);

        foreach (var (field, declarator, option) in fieldsArray)
            potentialLockFields[field] = (declarator, option, canUse: true, wasLocked: false);

        return potentialLockFields;
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context, PotentialLockFields potentialLockFields)
    {
        var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;
        var fieldReference = fieldReferenceOperation.Field.OriginalDefinition;

        // We only care about examining field references to the fields we're considering converting to System.Threading.Lock.
        ref var valueRef = ref GetValueRefOrNullRef(potentialLockFields, fieldReference);
        if (Unsafe.IsNullRef(ref valueRef))
            return;

        // If some other analysis already determined we can't convert this field, then no point continuing.
        if (!valueRef.canUse)
            return;

        if (fieldReferenceOperation.Parent is ILockOperation lockOperation)
        {
            // Locking on the the new lock type disallows yielding inside the lock.  So if we see that, immediately
            // consider this not applicable.
            if (lockOperation.Syntax.ContainsYield())
            {
                valueRef.canUse = false;
                return;
            }

            // We did lock on this field, mark as such as its now something we'd def like to convert to a
            // System.Threading.Lock if possible.
            valueRef.wasLocked = true;
            return;
        }

        // It's ok to assign to the field, as long as we're assigning a new lock object to it. e.g.  `_gate = new
        // object()` is fine to continue converting over to System.Threading.Lock.  But an assignment of something
        // else is not.
        if (fieldReferenceOperation.Parent is IAssignmentOperation assignment &&
            assignment.Target == fieldReferenceOperation &&
            IsObjectCreationOperation(assignment.Value))
        {
            return;
        }

        // Fine to use `nameof(someLock)` as that's not actually using the lock.
        if (fieldReferenceOperation.Parent is INameOfOperation)
            return;

        // Note: More patterns can be added here as needed.

        // This wasn't a supported case.  Immediately disallow conversion of this field.
        valueRef.canUse = false;
    }

    private static bool IsObjectCreationOperation(IOperation value)
        // unwrap the implicit conversion around `new()` if necessary.
        => value.UnwrapImplicitConversion() is IObjectCreationOperation { Type.SpecialType: SpecialType.System_Object };
}
