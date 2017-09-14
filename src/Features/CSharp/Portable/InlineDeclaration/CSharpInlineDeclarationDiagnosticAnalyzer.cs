// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    /// <summary>
    /// Looks for code of the form:
    /// 
    ///     int i;
    ///     if (int.TryParse(s, out i)) { }
    ///     
    /// And offers to convert it to:
    /// 
    ///     if (int.TryParse(s, out var i)) { }   or
    ///     if (int.TryParse(s, out int i)) { }   or
    /// 
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpInlineDeclarationDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private const string CS0165 = nameof(CS0165); // Use of unassigned local variable 's'

        public CSharpInlineDeclarationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineDeclarationDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Inline_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Variable_declaration_can_be_inlined), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var compilation = compilationContext.Compilation;
                var expressionTypeOpt = compilation.GetTypeByMetadataName(typeof(Expression<>).FullName);
                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeSyntaxNode(syntaxContext, expressionTypeOpt), SyntaxKind.Argument);
            });
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol expressionTypeOpt)
        {
            var argumentNode = (ArgumentSyntax)context.Node;
            var csOptions = (CSharpParseOptions)context.Node.SyntaxTree.Options;
            if (csOptions.LanguageVersion < LanguageVersion.CSharp7)
            {
                // out-vars are not supported prior to C# 7.0.
                return;
            }

            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }
            
            var option = optionSet.GetOption(CodeStyleOptions.PreferInlinedVariableDeclaration, argumentNode.Language);
            if (!option.Value)
            {
                // Don't bother doing any work if the user doesn't even have this preference set.
                return;
            }

            if (argumentNode.RefOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
            {
                // Immediately bail if this is not an out-argument.  If it's not an out-argument
                // we clearly can't convert it to be an out-variable-declaration.
                return;
            }

            var argumentExpression = argumentNode.Expression;
            if (argumentExpression.Kind() != SyntaxKind.IdentifierName)
            {
                // has to be exactly the form "out i".  i.e. "out this.i" or "out v[i]" are legal
                // cases for out-arguments, but could not be converted to an out-variable-declaration.
                return;
            }

            var argumentList = argumentNode.Parent as ArgumentListSyntax;
            if (argumentList == null)
            {
                return;
            }

            var invocationOrCreation = argumentList.Parent;
            if (!invocationOrCreation.IsKind(SyntaxKind.InvocationExpression) &&
                !invocationOrCreation.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                // Out-variables are only legal with invocations and object creations.
                // If we don't have one of those bail.  Note: we need hte parent to be
                // one of these forms so we can accurately verify that inlining the 
                // variable doesn't change semantics.
                return;
            }

            var identifierName = (IdentifierNameSyntax)argumentExpression;

            // Don't offer to inline variables named "_".  It can cause is to create a discard symbol
            // which would cause a break.
            if (identifierName.Identifier.ValueText == "_")
            {
                return;
            }

            var containingStatement = argumentExpression.FirstAncestorOrSelf<StatementSyntax>();
            if (containingStatement == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var outLocalSymbol = semanticModel.GetSymbolInfo(argumentExpression, cancellationToken).Symbol as ILocalSymbol;
            if (outLocalSymbol == null)
            {
                // The out-argument wasn't referencing a local.  So we don't have an local
                // declaration that we can attempt to inline here.
                return;
            }

            // Ensure that the local-symbol actually belongs to LocalDeclarationStatement.
            // Trying to do things like inline a var-decl in a for-statement is just too 
            // esoteric and would make us have to write a lot more complex code to support
            // that scenario.
            var localReference = outLocalSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var localDeclarator = localReference?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            if (localDeclarator == null)
            {
                return;
            }

            var localDeclaration = localDeclarator.Parent as VariableDeclarationSyntax;
            var localStatement = localDeclaration?.Parent as LocalDeclarationStatementSyntax;
            if (localStatement == null)
            {
                return;
            }

            if (localDeclarator.SpanStart >= argumentNode.SpanStart)
            {
                // We have an error situation where the local was declared after the out-var.  
                // Don't even bother offering anything here.
                return;
            }

            // If the local has an initializer, only allow the refactoring if it is initialized
            // with a simple literal or 'default' expression.  i.e. it's ok to inline "var v = 0"
            // since there are no side-effects of the initialization.  However something like
            // "var v = M()" should not be inlined as that could break program semantics.
            if (localDeclarator.Initializer != null)
            {
                if (!(localDeclarator.Initializer.Value is LiteralExpressionSyntax) &&
                    !(localDeclarator.Initializer.Value is DefaultExpressionSyntax))
                {
                    return;
                }
            }

            // Get the block that the local is scoped inside of.  We'll search that block
            // for references to the local to make sure that no reads/writes happen before
            // the out-argument.  If there are any reads/writes we can't inline as those
            // accesses will become invalid.
            var enclosingBlockOfLocalStatement = localStatement.Parent as BlockSyntax;
            if (enclosingBlockOfLocalStatement == null)
            {
                return;
            }

            if (argumentExpression.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken))
            {
                // out-vars are not allowed inside expression-trees.  So don't offer to
                // fix if we're inside one.
                return;
            }

            // Find the scope that the out-declaration variable will live in after we
            // rewrite things.
            var outArgumentScope = GetOutArgumentScope(argumentExpression);

            if (enclosingBlockOfLocalStatement != outArgumentScope &&
                enclosingBlockOfLocalStatement.Parent.IsKind(SyntaxKind.LocalFunctionStatement, SyntaxKind.MethodDeclaration) &&
                outArgumentScope.Parent.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                if (HasTypeParameter(enclosingBlockOfLocalStatement.Parent, outLocalSymbol) &&
                    HasTypeParameter(outArgumentScope.Parent, outLocalSymbol))
                {
                    return;
                }
            }

            // Make sure that variable is not accessed outside of that scope.
            var dataFlow = semanticModel.AnalyzeDataFlow(outArgumentScope);
            if (dataFlow.ReadOutside.Contains(outLocalSymbol) || dataFlow.WrittenOutside.Contains(outLocalSymbol))
            {
                // The variable is read or written from outside the block that the new variable
                // would be scoped in.  This would cause a break.
                //
                // Note(cyrusn): We could still offer the refactoring, but just show an error in the
                // preview in this case.
                return;
            }

            // Make sure the variable isn't ever accessed before the usage in this out-var.
            if (IsAccessed(semanticModel, outLocalSymbol, enclosingBlockOfLocalStatement, 
                           localStatement, argumentNode, cancellationToken))
            {
                return;
            }

            // See if inlining this variable would make it so that some variables were no
            // longer definitely assigned.
            if (WouldCauseDefiniteAssignmentErrors(
                    semanticModel, localDeclaration, localDeclarator, 
                    enclosingBlockOfLocalStatement, outLocalSymbol, cancellationToken))
            {
                return;
            }

            // Collect some useful nodes for the fix provider to use so it doesn't have to
            // find them again.
            var allLocations = ImmutableArray.Create(
                localDeclarator.GetLocation(),
                identifierName.GetLocation(),
                invocationOrCreation.GetLocation(),
                containingStatement.GetLocation());

            // If the local variable only has one declarator, then report the suggestion on the whole
            // declaration.  Otherwise, return the suggestion only on the single declarator.
            var reportNode = localDeclaration.Variables.Count == 1
                ? (SyntaxNode)localDeclaration
                : localDeclarator;

            context.ReportDiagnostic(Diagnostic.Create(
                GetDescriptorWithSeverity(option.Notification.Value),
                reportNode.GetLocation(),
                additionalLocations: allLocations));
        }

        private bool WouldCauseDefiniteAssignmentErrors(
            SemanticModel semanticModel, 
            VariableDeclarationSyntax localDeclaration,
            VariableDeclaratorSyntax localDeclarator, 
            BlockSyntax enclosingBlock,
            ILocalSymbol outLocalSymbol,
            CancellationToken cancellationToken)
        {
            // See if we have something like:
            //
            //      int i = 0;
            //      if (Goo() || Bar(out i))
            //      {
            //          Console.WriteLine(i);
            //      }
            //
            // In this case, inlining the 'i' would cause it to longer be definitely
            // assigned in the WriteLine invocation.

            // Find all the current read-references to the local.
            var query = from t in enclosingBlock.DescendantTokens()
                        where t.Kind() == SyntaxKind.IdentifierToken
                        where t.ValueText == outLocalSymbol.Name
                        let id = t.Parent as IdentifierNameSyntax
                        where id != null
                        where !id.IsOnlyWrittenTo()
                        let symbol = semanticModel.GetSymbolInfo(id).GetAnySymbol()
                        where outLocalSymbol.Equals(symbol)
                        select id;

            var references = query.ToImmutableArray<SyntaxNode>();

            var root = semanticModel.SyntaxTree.GetCompilationUnitRoot(cancellationToken);

            // Ensure we can track the references and the local variable as we make edits
            // to the tree.
            var rootWithTrackedNodes = root.TrackNodes(
                references.Concat(ImmutableArray.Create<SyntaxNode>(localDeclarator, localDeclaration, enclosingBlock)));

            // Now, take the local variable and remove it's initializer.  Then go to all
            // the locations where we read from it.  If they're definitely assigned, then
            // that means the out-var did it's work and assigned the variable across all
            // paths. If it's not definitely assigned, then we can't inline this variable.
            var currentLocalDeclarator = rootWithTrackedNodes.GetCurrentNode(localDeclarator);
            var currentLocalDeclaration = rootWithTrackedNodes.GetCurrentNode(localDeclaration);
            var updatedDeclaration = currentLocalDeclaration
                .ReplaceNode(currentLocalDeclarator, currentLocalDeclarator.WithInitializer(null));

            // If the declaration was a "var" declaration, then replace "var" with the actual
            // type of the local.  This way we don't get a "'var v' requires an initializer" which
            // will suppress the message about definite assignment later.
            if (updatedDeclaration.Type.IsVar)
            {
                updatedDeclaration = updatedDeclaration.WithType(
                    outLocalSymbol.Type.GenerateTypeSyntax());
            }

            var rootWithoutInitializer = rootWithTrackedNodes.ReplaceNode(
                currentLocalDeclaration, updatedDeclaration);

            var rootWithoutInitializerTree = root.SyntaxTree.WithRootAndOptions(
                rootWithoutInitializer, root.SyntaxTree.Options);

            // Fork the compilation so we can do this analysis.
            var newCompilation = semanticModel.Compilation.ReplaceSyntaxTree(
                root.SyntaxTree, rootWithoutInitializerTree);
            var newSemanticModel = newCompilation.GetSemanticModel(rootWithoutInitializerTree);

            // NOTE: there is no current compiler API to determine if a variable is definitely
            // assigned or not.  So, for now, we just get diagnostics for this block and see if
            // we get any definite assignment errors where we have a reference to the symbol. If
            // so, then we don't offer the fix.

            rootWithoutInitializer = (CompilationUnitSyntax)rootWithoutInitializerTree.GetRoot(cancellationToken);
            var currentBlock = rootWithoutInitializer.GetCurrentNode(enclosingBlock);
            var diagnostics = newSemanticModel.GetDiagnostics(currentBlock.Span, cancellationToken);

            var diagnosticSpans = diagnostics.Where(d => d.Id == CS0165)
                                             .Select(d => d.Location.SourceSpan)
                                             .Distinct();

            var newReferenceSpans = rootWithoutInitializer.GetCurrentNodes<SyntaxNode>(references)
                                                          .Select(n => n.Span)
                                                          .Distinct();

            return diagnosticSpans.Intersect(newReferenceSpans).Any();
        }

        private SyntaxNode GetOutArgumentScope(SyntaxNode argumentExpression)
        {
            for (var current = argumentExpression; current != null; current = current.Parent)
            {
                if (current.Parent is LambdaExpressionSyntax lambda &&
                    current == lambda.Body)
                {
                    // We were in a lambda.  The lambda body will be the new scope of the 
                    // out var.
                    return current;
                }

                // Any loop construct defines a scope for out-variables, as well as each of the following:
                // * Using statements
                // * Fixed statements
                // * Try statements (specifically for exception filters)
                if (current.Kind() == SyntaxKind.WhileStatement ||
                    current.Kind() == SyntaxKind.DoStatement ||
                    current.Kind() == SyntaxKind.ForStatement ||
                    current.Kind() == SyntaxKind.ForEachStatement ||
                    current.Kind() == SyntaxKind.UsingStatement ||
                    current.Kind() == SyntaxKind.FixedStatement ||
                    current.Kind() == SyntaxKind.TryStatement)
                {
                    return current;
                }

                if (current is StatementSyntax)
                {
                    // We hit a statement containing the out-argument.  Statements can have one of 
                    // two forms.  They're either parented by a block, or by another statement 
                    // (i.e. they're an embedded statement).  If we're parented by a block, then
                    // that block will be the scope of the new out-var.
                    //
                    // However, if our containing statement is not parented by a block, then that
                    // means we have something like:
                    //
                    //      if (x)
                    //          if (Try(out y))
                    //
                    // In this case, there is a 'virtual' block scope surrounding the embedded 'if'
                    // statement, and that will be the scope the out-var goes into.
                    return current.IsParentKind(SyntaxKind.Block)
                        ? current.Parent
                        : current;
                }
            }

            return null;
        }

        private bool IsAccessed(
            SemanticModel semanticModel,
            ISymbol outSymbol, 
            BlockSyntax enclosingBlockOfLocalStatement,
            LocalDeclarationStatementSyntax localStatement, 
            ArgumentSyntax argumentNode,
            CancellationToken cancellationToken)
        {
            var localStatementStart = localStatement.Span.Start;
            var argumentNodeStart = argumentNode.Span.Start;
            var variableName = outSymbol.Name;

            // Walk the block that the local is declared in looking for accesses.
            // We can ignore anything prior to the actual local declaration point,
            // and we only need to check up until we reach the out-argument.
            foreach (var descendentNode in enclosingBlockOfLocalStatement.DescendantNodes())
            {
                var descendentStart = descendentNode.Span.Start;
                if (descendentStart <= localStatementStart)
                {
                    // This node is before the local declaration.  Can ignore it entirely as it could
                    // not be an access to the local.
                    continue;
                }

                if (descendentStart >= argumentNodeStart)
                {
                    // We reached the out-var.  We can stop searching entirely.
                    break;
                }

                if (descendentNode.IsKind(SyntaxKind.IdentifierName))
                {
                    // See if this looks like an accessor to the local variable syntactically.
                    var identifierName = (IdentifierNameSyntax)descendentNode;
                    if (identifierName.Identifier.ValueText == variableName)
                    {
                        // Confirm that it is a access of the local.
                        var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
                        if (outSymbol.Equals(symbol))
                        {
                            // We definitely accessed the local before the out-argument.  We 
                            // can't inline this local.
                            return true;
                        }
                    }
                }
            }

            // No accesses detected
            return false;
        }

        private static bool HasTypeParameter(SyntaxNode node, ILocalSymbol symbol)
        {
            SeparatedSyntaxList<TypeParameterSyntax> typeParameters;
            switch (node)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    typeParameters = methodDeclaration.TypeParameterList.Parameters;
                    break;
                case LocalFunctionStatementSyntax localFunctionStatement:
                    typeParameters = localFunctionStatement.TypeParameterList.Parameters;
                    break;
                default:
                    return false;
            }

            return typeParameters.Any(x => x.Identifier.ValueText == symbol.Type.Name);
        }
    }
}
