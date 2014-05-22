// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        internal struct ProcessedFieldInitializers
        {
            internal ImmutableArray<BoundInitializer> BoundInitializers { get; set; }
            internal BoundStatementList LoweredInitializers { get; set; }
            internal bool HasErrors { get; set; }
            internal ConsList<Imports> FirstDebugImports { get; set; }
        }

        internal static void BindFieldInitializers(
            SourceMemberContainerTypeSymbol typeSymbol,
            MethodSymbol scriptCtor,
            ImmutableArray<FieldInitializers> fieldInitializers,
            bool generateDebugInfo,
            DiagnosticBag diagnostics,
            ref ProcessedFieldInitializers processedInitializers) //by ref so that we can store the results of lowering
        {
            DiagnosticBag diagsForInstanceInitializers = DiagnosticBag.GetInstance();
            try
            {
                ConsList<Imports> firstDebugImports;

                processedInitializers.BoundInitializers = BindFieldInitializers(typeSymbol, scriptCtor, fieldInitializers,
                    diagsForInstanceInitializers, generateDebugInfo, out firstDebugImports);

                processedInitializers.HasErrors = diagsForInstanceInitializers.HasAnyErrors();
                processedInitializers.FirstDebugImports = firstDebugImports;
            }
            finally
            {
                diagnostics.AddRange(diagsForInstanceInitializers);
                diagsForInstanceInitializers.Free();
            }
        }

        internal static ImmutableArray<BoundInitializer> BindFieldInitializers(
            SourceMemberContainerTypeSymbol containingType,
            MethodSymbol scriptCtor,
            ImmutableArray<FieldInitializers> initializers,
            DiagnosticBag diagnostics,
            bool generateDebugInfo,
            out ConsList<Imports> firstDebugImports)
        {
            if (initializers.IsEmpty)
            {
                firstDebugImports = null;
                return ImmutableArray<BoundInitializer>.Empty;
            }

            CSharpCompilation compilation = containingType.DeclaringCompilation;
            ArrayBuilder<BoundInitializer> boundInitializers = ArrayBuilder<BoundInitializer>.GetInstance();

            if ((object)scriptCtor == null)
            {
                BindRegularCSharpFieldInitializers(compilation, initializers, boundInitializers, diagnostics, generateDebugInfo, out firstDebugImports);
            }
            else
            {
                BindScriptFieldInitializers(compilation, scriptCtor, initializers, boundInitializers, diagnostics, generateDebugInfo, out firstDebugImports);
            }

            return boundInitializers.ToImmutableAndFree();
        }

        private struct FieldInitializerInfo
        {
            public readonly FieldInitializer Initializer;
            public readonly Binder Binder;
            public readonly EqualsValueClauseSyntax EqualsValue;

            public FieldInitializerInfo(FieldInitializer initializer, Binder binder, EqualsValueClauseSyntax equalsValue)
            {
                Initializer = initializer;
                Binder = binder;
                EqualsValue = equalsValue;
            }
        }

        /// <summary>
        /// In regular C#, all field initializers are assignments to fields and the assigned expressions
        /// may not reference instance members.
        /// </summary>
        private static void BindRegularCSharpFieldInitializers(
            CSharpCompilation compilation,
            ImmutableArray<FieldInitializers> initializers,
            ArrayBuilder<BoundInitializer> boundInitializers,
            DiagnosticBag diagnostics,
            bool generateDebugInfo,
            out ConsList<Imports> firstDebugImports)
        {
            firstDebugImports = null;

            foreach (FieldInitializers siblingInitializers in initializers)
            {
                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;

                var infos = ArrayBuilder<FieldInitializerInfo>.GetInstance(); // Exact size is not known up front.

                foreach (FieldInitializer initializer in siblingInitializers.Initializers)
                {
                    FieldSymbol fieldSymbol = initializer.Field;
                    Debug.Assert((object)fieldSymbol != null);

                    // A constant field of type decimal needs a field initializer, so
                    // check if it is a metadata constant, not just a constant to exclude
                    // decimals. Other constants do not need field initializers.
                    if (!fieldSymbol.IsMetadataConstant)
                    {
                        //Can't assert that this is a regular C# compilation, because we could be in a nested type of a script class.
                        SyntaxReference syntaxRef = initializer.Syntax;
                        var initializerNode = (EqualsValueClauseSyntax)syntaxRef.GetSyntax();

                        if (binderFactory == null)
                        {
                            binderFactory = compilation.GetBinderFactory(syntaxRef.SyntaxTree);
                        }

                        Binder parentBinder = binderFactory.GetBinder(initializerNode);
                        Debug.Assert(parentBinder.ContainingMemberOrLambda == fieldSymbol.ContainingType || //should be the binder for the type
                            fieldSymbol.ContainingType.IsImplicitClass); //however, we also allow fields in namespaces to help support script scenarios

                        if (generateDebugInfo && firstDebugImports == null)
                        {
                            firstDebugImports = parentBinder.ImportsList;
                        }

                        parentBinder = new LocalScopeBinder(parentBinder).WithAdditionalFlagsAndContainingMemberOrLambda(parentBinder.Flags | BinderFlags.FieldInitializer, fieldSymbol);

                        if (!fieldSymbol.IsConst && !fieldSymbol.IsStatic)
                        {
                            parentBinder = parentBinder.WithPrimaryConstructorParametersIfNecessary(fieldSymbol.ContainingType);
                        }

                        infos.Add(new FieldInitializerInfo(initializer, parentBinder, initializerNode));
                    }
                }

                // See if there are locals that we need to bring into the scope.
                var locals = default(ImmutableArray<LocalSymbol>);
                if (siblingInitializers.TypeDeclarationSyntax != null)
                {
                    locals = GetInitializationScopeLocals(infos);
                }

                ArrayBuilder<BoundInitializer> initializersBuilder = locals.IsDefaultOrEmpty ? boundInitializers : ArrayBuilder<BoundInitializer>.GetInstance(infos.Count);

                foreach (var info in infos)
                {
                    Binder binder = info.Binder;
                    ScopedExpressionBinder scopedExpressionBinder = null;

                    // Constant initializers is not part of the initialization scope.
                    if (info.Initializer.Field.IsConst || locals.IsDefault)
                    {
                        binder = scopedExpressionBinder = new ScopedExpressionBinder(binder, info.EqualsValue.Value);
                    }
                    else if (!locals.IsEmpty)
                    {
                        binder = new SimpleLocalScopeBinder(locals, binder);
                    }

                    BoundFieldInitializer boundInitializer = BindFieldInitializer(binder, info.Initializer.Field, info.EqualsValue, diagnostics);

                    if (scopedExpressionBinder != null && !scopedExpressionBinder.Locals.IsDefaultOrEmpty)
                    {
                        boundInitializer = boundInitializer.Update(boundInitializer.Field, scopedExpressionBinder.AddLocalScopeToExpression(boundInitializer.InitialValue));
                    }

                    initializersBuilder.Add(boundInitializer);
                }

                Debug.Assert(locals.IsDefaultOrEmpty == (initializersBuilder == boundInitializers));
                if (!locals.IsDefaultOrEmpty)
                {
                    boundInitializers.Add(new BoundInitializationScope((CSharpSyntaxNode)siblingInitializers.TypeDeclarationSyntax.GetSyntax(),
                                                                       locals, initializersBuilder.ToImmutableAndFree()));
                }
            }
        }

        private static ImmutableArray<LocalSymbol> GetInitializationScopeLocals(ArrayBuilder<FieldInitializerInfo> infos)
        {
            ArrayBuilder<LocalSymbol> localsBuilder = null;

            foreach (var info in infos)
            {
                // Constant initializers do not contribute to the initialization scope.
                if (!info.Initializer.Field.IsConst)
                {
                    var walker = new LocalScopeBinder.BuildLocalsFromDeclarationsWalker(info.Binder);
                    walker.Visit(info.EqualsValue.Value);

                    if (walker.Locals != null)
                    {
                        if (localsBuilder == null)
                        {
                            localsBuilder = walker.Locals;
                        }
                        else
                        {
                            localsBuilder.AddRange(walker.Locals);
                            walker.Locals.Free();
                        }
                    }
                }
            }

            if (localsBuilder != null)
            {
                return localsBuilder.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        /// <summary>
        /// In script C#, some field initializers are assignments to fields and others are global
        /// statements.  There are no restrictions on accessing instance members.
        /// </summary>
        private static void BindScriptFieldInitializers(CSharpCompilation compilation, MethodSymbol scriptCtor,
            ImmutableArray<FieldInitializers> initializers, ArrayBuilder<BoundInitializer> boundInitializers, DiagnosticBag diagnostics,
            bool generateDebugInfo, out ConsList<Imports> firstDebugImports)
        {
            Debug.Assert((object)scriptCtor != null);

            firstDebugImports = null;

            for (int i = 0; i < initializers.Length; i++)
            {
                FieldInitializers siblingInitializers = initializers[i];

                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;

                for (int j = 0; j < siblingInitializers.Initializers.Length; j++)
                {
                    Debug.Assert(siblingInitializers.TypeDeclarationSyntax == null);
                    var initializer = siblingInitializers.Initializers[j];
                    var fieldSymbol = initializer.Field;

                    if ((object)fieldSymbol != null && fieldSymbol.IsConst)
                    {
                        // Constants do not need field initializers.
                        continue;
                    }

                    var syntaxRef = initializer.Syntax;
                    Debug.Assert(syntaxRef.SyntaxTree.Options.Kind != SourceCodeKind.Regular);

                    var initializerNode = (CSharpSyntaxNode)syntaxRef.GetSyntax();

                    if (binderFactory == null)
                    {
                        binderFactory = compilation.GetBinderFactory(syntaxRef.SyntaxTree);
                    }

                    Binder scriptClassBinder = binderFactory.GetBinder(initializerNode);
                    Debug.Assert(((ImplicitNamedTypeSymbol)scriptClassBinder.ContainingMemberOrLambda).IsScriptClass);

                    if (generateDebugInfo && firstDebugImports == null)
                    {
                        firstDebugImports = scriptClassBinder.ImportsList;
                    }

                    Binder parentBinder = new ExecutableCodeBinder((CSharpSyntaxNode)syntaxRef.SyntaxTree.GetRoot(), scriptCtor, scriptClassBinder);

                    BoundInitializer boundInitializer;
                    if ((object)fieldSymbol != null)
                    {
                        boundInitializer = BindFieldInitializer(
                            new LocalScopeBinder(parentBinder).WithAdditionalFlagsAndContainingMemberOrLambda(parentBinder.Flags | BinderFlags.FieldInitializer, fieldSymbol),
                            fieldSymbol,
                            (EqualsValueClauseSyntax)initializerNode,
                            diagnostics);
                    }
                    else if (initializerNode.Kind == SyntaxKind.LabeledStatement)
                    {
                        // TODO: labels in interactive
                        var boundStatement = new BoundBadStatement(initializerNode, ImmutableArray<BoundNode>.Empty, true);
                        boundInitializer = new BoundGlobalStatementInitializer(initializerNode, boundStatement);
                    }
                    else
                    {
                        var collisionDetector = new LocalScopeBinder(parentBinder);
                        boundInitializer = BindGlobalStatement(collisionDetector, (StatementSyntax)initializerNode, diagnostics,
                            isLast: i == initializers.Length - 1 && j == siblingInitializers.Initializers.Length - 1);
                    }

                    boundInitializers.Add(boundInitializer);
                }
            }
        }

        private static BoundInitializer BindGlobalStatement(Binder binder, StatementSyntax statementNode, DiagnosticBag diagnostics, bool isLast)
        {
            BoundStatement boundStatement = binder.BindStatement(statementNode, diagnostics);

            // the result of the last global expression is assigned to the result storage for submission result:
            if (binder.Compilation.IsSubmission && isLast && boundStatement.Kind == BoundKind.ExpressionStatement && !boundStatement.HasAnyErrors)
            {
                var submissionReturnType = binder.Compilation.GetSubmissionReturnType();

                // insert an implicit conversion for the submission return type (if needed):
                var expression = ((BoundExpressionStatement)boundStatement).Expression;
                if ((object)expression.Type == null || expression.Type.SpecialType != SpecialType.System_Void)
                {
                    expression = binder.GenerateConversionForAssignment(submissionReturnType, expression, diagnostics);
                    boundStatement = new BoundExpressionStatement(boundStatement.Syntax, expression, expression.HasErrors);
                }
            }

            return new BoundGlobalStatementInitializer(statementNode, boundStatement);
        }

        private static BoundFieldInitializer BindFieldInitializer(Binder binder, FieldSymbol fieldSymbol, EqualsValueClauseSyntax equalsValueClauseNode,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(!fieldSymbol.IsMetadataConstant);

            var fieldsBeingBound = binder.FieldsBeingBound;

            var sourceField = fieldSymbol as SourceMemberFieldSymbol;
            bool isImplicitlyTypedField = (object)sourceField != null && sourceField.FieldTypeInferred(fieldsBeingBound);

            // If the type is implicitly typed, the initializer diagnostics have already been reported, so ignore them here:
            // CONSIDER (tomat): reusing the bound field initializers for implicitly typed fields.
            DiagnosticBag initializerDiagnostics;
            if (isImplicitlyTypedField)
            {
                initializerDiagnostics = DiagnosticBag.GetInstance();
            }
            else
            {
                initializerDiagnostics = diagnostics;
            }

            var collisionDetector = new LocalScopeBinder(binder);
            var boundInitValue = collisionDetector.BindVariableOrAutoPropInitializer(equalsValueClauseNode, fieldSymbol.GetFieldType(fieldsBeingBound), initializerDiagnostics);

            if (isImplicitlyTypedField)
            {
                initializerDiagnostics.Free();
            }

            return new BoundFieldInitializer(
                equalsValueClauseNode.Value, //we want the attached sequence point to indicate the value node
                fieldSymbol,
                boundInitValue);
        }
    }
}
