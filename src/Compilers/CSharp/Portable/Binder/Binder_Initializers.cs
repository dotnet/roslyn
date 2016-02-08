// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal struct ProcessedFieldInitializers
        {
            internal ImmutableArray<BoundInitializer> BoundInitializers { get; set; }
            internal BoundStatementList LoweredInitializers { get; set; }
            internal bool HasErrors { get; set; }
            internal ImportChain FirstImportChain { get; set; }
        }

        internal static void BindFieldInitializers(
            CSharpCompilation compilation,
            SynthesizedInteractiveInitializerMethod scriptInitializerOpt,
            ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> fieldInitializers,
            DiagnosticBag diagnostics,
            ref ProcessedFieldInitializers processedInitializers)
        {
            var diagsForInstanceInitializers = DiagnosticBag.GetInstance();
            ImportChain firstImportChain;
            processedInitializers.BoundInitializers = BindFieldInitializers(compilation, scriptInitializerOpt, fieldInitializers, diagsForInstanceInitializers, out firstImportChain);
            processedInitializers.HasErrors = diagsForInstanceInitializers.HasAnyErrors();
            processedInitializers.FirstImportChain = firstImportChain;
            diagnostics.AddRange(diagsForInstanceInitializers);
            diagsForInstanceInitializers.Free();
        }

        private static ImmutableArray<BoundInitializer> BindFieldInitializers(
            CSharpCompilation compilation,
            SynthesizedInteractiveInitializerMethod scriptInitializerOpt,
            ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers,
            DiagnosticBag diagnostics,
            out ImportChain firstImportChain)
        {
            if (initializers.IsEmpty)
            {
                firstImportChain = null;
                return ImmutableArray<BoundInitializer>.Empty;
            }

            var boundInitializers = ArrayBuilder<BoundInitializer>.GetInstance();
            if ((object)scriptInitializerOpt == null)
            {
                BindRegularCSharpFieldInitializers(compilation, initializers, boundInitializers, diagnostics, out firstImportChain);
            }
            else
            {
                BindScriptFieldInitializers(compilation, scriptInitializerOpt, initializers, boundInitializers, diagnostics, out firstImportChain);
            }
            return boundInitializers.ToImmutableAndFree();
        }

        /// <summary>
        /// In regular C#, all field initializers are assignments to fields and the assigned expressions
        /// may not reference instance members.
        /// </summary>
        internal static void BindRegularCSharpFieldInitializers(
            CSharpCompilation compilation,
            ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers,
            ArrayBuilder<BoundInitializer> boundInitializers,
            DiagnosticBag diagnostics,
            out ImportChain firstDebugImports)
        {
            firstDebugImports = null;

            foreach (ImmutableArray<FieldOrPropertyInitializer> siblingInitializers in initializers)
            {
                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;

                foreach (FieldOrPropertyInitializer initializer in siblingInitializers)
                {
                    FieldSymbol fieldSymbol = initializer.FieldOpt;
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

                        if (firstDebugImports == null)
                        {
                            firstDebugImports = parentBinder.ImportChain;
                        }

                        parentBinder = new LocalScopeBinder(parentBinder).WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.FieldInitializer, fieldSymbol);

                        BoundFieldInitializer boundInitializer = BindFieldInitializer(parentBinder, fieldSymbol, initializerNode, diagnostics);
                        boundInitializers.Add(boundInitializer);
                    }
                }
            }
        }

        /// <summary>
        /// In script C#, some field initializers are assignments to fields and others are global
        /// statements.  There are no restrictions on accessing instance members.
        /// </summary>
        private static void BindScriptFieldInitializers(
            CSharpCompilation compilation,
            SynthesizedInteractiveInitializerMethod scriptInitializer,
            ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers,
            ArrayBuilder<BoundInitializer> boundInitializers,
            DiagnosticBag diagnostics,
            out ImportChain firstDebugImports)
        {
            firstDebugImports = null;

            for (int i = 0; i < initializers.Length; i++)
            {
                ImmutableArray<FieldOrPropertyInitializer> siblingInitializers = initializers[i];

                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;
                // Label instances must be shared across all global statements.
                ScriptLocalScopeBinder.Labels labels = null;

                for (int j = 0; j < siblingInitializers.Length; j++)
                {
                    var initializer = siblingInitializers[j];
                    var fieldSymbol = initializer.FieldOpt;

                    if ((object)fieldSymbol != null && fieldSymbol.IsConst)
                    {
                        // Constants do not need field initializers.
                        continue;
                    }

                    var syntaxRef = initializer.Syntax;
                    var syntaxTree = syntaxRef.SyntaxTree;
                    Debug.Assert(syntaxTree.Options.Kind != SourceCodeKind.Regular);

                    var syntax = (CSharpSyntaxNode)syntaxRef.GetSyntax();
                    var syntaxRoot = syntaxTree.GetCompilationUnitRoot();

                    if (binderFactory == null)
                    {
                        binderFactory = compilation.GetBinderFactory(syntaxTree);
                        labels = new ScriptLocalScopeBinder.Labels(scriptInitializer, syntaxRoot);
                    }

                    Binder scriptClassBinder = binderFactory.GetBinder(syntax);
                    Debug.Assert(((NamedTypeSymbol)scriptClassBinder.ContainingMemberOrLambda).IsScriptClass);

                    if (firstDebugImports == null)
                    {
                        firstDebugImports = scriptClassBinder.ImportChain;
                    }

                    Binder parentBinder = new ExecutableCodeBinder(
                        syntaxRoot,
                        scriptInitializer,
                        new ScriptLocalScopeBinder(labels, scriptClassBinder));

                    BoundInitializer boundInitializer;
                    if ((object)fieldSymbol != null)
                    {
                        boundInitializer = BindFieldInitializer(
                            parentBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.FieldInitializer, fieldSymbol),
                            fieldSymbol,
                            (EqualsValueClauseSyntax)syntax,
                            diagnostics);
                    }
                    else
                    {
                        boundInitializer = BindGlobalStatement(
                            parentBinder,
                            scriptInitializer,
                            (StatementSyntax)syntax,
                            diagnostics,
                            isLast: i == initializers.Length - 1 && j == siblingInitializers.Length - 1);
                    }

                    boundInitializers.Add(boundInitializer);
                }
            }
        }

        private static BoundInitializer BindGlobalStatement(
            Binder binder,
            SynthesizedInteractiveInitializerMethod scriptInitializer,
            StatementSyntax statementNode,
            DiagnosticBag diagnostics,
            bool isLast)
        {
            var statement = binder.BindStatement(statementNode, diagnostics);
            if (isLast && !statement.HasAnyErrors)
            {
                // the result of the last global expression is assigned to the result storage for submission result:
                if (binder.Compilation.IsSubmission)
                {
                    // insert an implicit conversion for the submission return type (if needed):
                    var expression = InitializerRewriter.GetTrailingScriptExpression(statement);
                    if (expression != null &&
                        ((object)expression.Type == null || expression.Type.SpecialType != SpecialType.System_Void))
                    {
                        var submissionResultType = scriptInitializer.ResultType;
                        expression = binder.GenerateConversionForAssignment(submissionResultType, expression, diagnostics);
                        statement = new BoundExpressionStatement(statement.Syntax, expression, expression.HasErrors);
                    }
                }

                // don't allow trailing expressions after labels (as in regular C#, labels must be followed by a statement):
                if (statement.Kind == BoundKind.LabeledStatement)
                {
                    var labeledStatementBody = ((BoundLabeledStatement)statement).Body;
                    while (labeledStatementBody.Kind == BoundKind.LabeledStatement)
                    {
                        labeledStatementBody = ((BoundLabeledStatement)labeledStatementBody).Body;
                    }

                    if (InitializerRewriter.GetTrailingScriptExpression(labeledStatementBody) != null)
                    {
                        Error(diagnostics, ErrorCode.ERR_SemicolonExpected, ((ExpressionStatementSyntax)labeledStatementBody.Syntax).SemicolonToken);
                    }
                }
            }

            return new BoundGlobalStatementInitializer(statementNode, statement);
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
            var boundInitValue = collisionDetector.BindVariableOrAutoPropInitializer(equalsValueClauseNode, 
                                                                                     binder.GetFieldTypeWithAdjustedNullableAnnotations(fieldSymbol, fieldsBeingBound), 
                                                                                     initializerDiagnostics);

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
