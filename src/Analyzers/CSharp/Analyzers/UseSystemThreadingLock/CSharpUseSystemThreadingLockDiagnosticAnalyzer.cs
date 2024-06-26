// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;

/// <summary>
/// Looks for code of the form:
/// 
///     object _gate = new object();
///     ...
///     lock (_gate)
///     {
///     }
///     
/// and converts it to:
/// 
///     Lock _gate = new Lock();
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseSystemThreadingLockDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseSystemThreadingLockDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseSystemThreadingLockDiagnosticId,
               EnforceOnBuildValues.UseSystemThreadingLock,
               CSharpCodeStyleOptions.PreferSystemThreadingLock,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Use_System_Threading_Lock), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;

            // The new 'Lock' feature is only supported in C# 13 and above.
            if (!compilation.LanguageVersion().IsCSharp13OrAbove())
                return;

            var lockType = compilation.GetTypeByMetadataName("System.Threading.Lock");
            if (lockType is null)
                return;

            context.RegisterSymbolStartAction(context => AnalyzeNamedType(context, lockType), SymbolKind.NamedType);
        });
    }

    private void AnalyzeNamedType(SymbolStartAnalysisContext context, INamedTypeSymbol lockType)
    {
        var cancellationToken = context.CancellationToken;
        if (lockType is not
            {
                TypeKind: TypeKind.Class or TypeKind.Struct,
                DeclaringSyntaxReferences: [var reference, ..]
            })
        {
            return;
        }

        var syntaxTree = reference.GetSyntax(cancellationToken).SyntaxTree;
        var option = context.GetCSharpAnalyzerOptions(syntaxTree).PreferSystemThreadingLock;

        // Bail immediately if the user has disabled this feature.
        if (!option.Value || ShouldSkipAnalysis(syntaxTree, context.Options, context.Compilation.Options, option.Notification, cancellationToken))
            return;

        // Needs to have a private field that is exactly typed as 'object'
        using var fieldsArray = TemporaryArray<IFieldSymbol>.Empty;

        foreach (var member in lockType.GetMembers())
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

            if (fieldSyntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax fieldSyntax)
                continue;

            // For simplicity, only offer this for fields with a single declarator.
            if (fieldSyntax.Parent is not VariableDeclarationSyntax { Parent: FieldDeclarationSyntax, Variables.Count: 1 })
                return;

            // If we have a private-object field, it needs to be initialized with either `new object()` or `new()`.
            if (fieldSyntax.Initializer != null)
            {
                if (fieldSyntax.Initializer.Value
                        is not ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 }
                        and not ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Type: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.ObjectKeyword } })
                {
                    continue;
                }
            }

            // Looks like something that could be converted to a lock if we see that this is used as a lock.
            fieldsArray.Add(field);
        }

        if (fieldsArray.Count == 0)
            return;

        var potentialLockFields = new ConcurrentSet<IFieldSymbol>();
        var wasLockedSet = new ConcurrentSet<IFieldSymbol>();
        foreach (var field in fieldsArray)
            potentialLockFields.Add(field);

        context.RegisterOperationAction(context =>
        {
            var cancellationToken = context.CancellationToken;
            var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;
            var fieldReference = fieldReferenceOperation.Field.OriginalDefinition;

            if (!potentialLockFields.Contains(fieldReference))
                return;

            if (fieldReferenceOperation.Parent is ILockOperation lockOperation)
            {
                // We did lock on this field, mark as such as its now something we'd def like to convert to a Lock if possible.
                wasLockedSet.Add(fieldReference);
                return;
            }

            // it's ok to assign to the field, as long as we're assigning a new lock object to it.
            if (fieldReferenceOperation.Parent is IAssignmentOperation
                {
                    Value: IObjectCreationOperation { Arguments.Length: 0, Constructor.ContainingType.SpecialType: SpecialType.System_Object },
                } assignment &&
                assignment.Target == fieldReferenceOperation)
            {
                return;
            }

            // Fine to use `nameof(someLock)` as that's not actually using the lock.
            if (fieldReferenceOperation.Parent is INameOfOperation)
                return;

            // Add more supported patterns here as needed.

            // This wasn't a supported case.
            potentialLockFields.Remove(fieldReference);
        }, OperationKind.FieldReference);

        context.RegisterSymbolEndAction(context =>
        {
            var cancellationToken = context.CancellationToken;

            foreach (var lockField in potentialLockFields)
            {
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
    }
}
