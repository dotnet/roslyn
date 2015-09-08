using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [Export]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseAutoPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string UseAutoProperty = nameof(UseAutoProperty);
        public const string UseAutoPropertyFadedToken = nameof(UseAutoPropertyFadedToken);

        private readonly static DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            UseAutoProperty, CSharpEditorResources.UseAutoProperty, CSharpEditorResources.UseAutoProperty, 
            "Language", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

        private readonly static DiagnosticDescriptor FadedTokenDescriptor = new DiagnosticDescriptor(
            UseAutoPropertyFadedToken, CSharpEditorResources.UseAutoProperty, CSharpEditorResources.UseAutoProperty, 
            "Language", DiagnosticSeverity.Hidden, isEnabledByDefault: true,
            customTags: new[] { WellKnownDiagnosticTags.Unnecessary });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor, FadedTokenDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(csac =>
            {
                var analysisResults = new ConcurrentBag<AnalysisResult>();
                var ineligibleFields = new ConcurrentBag<IFieldSymbol>();
                
                csac.RegisterSymbolAction(sac => AnalyzeProperty(analysisResults, sac), SymbolKind.Property);
                csac.RegisterSyntaxNodeAction(snac => AnalyzeArgument(ineligibleFields, snac), SyntaxKind.Argument);

                csac.RegisterCompilationEndAction(cac => Process(analysisResults, ineligibleFields, cac));
            });
        }
        
        private void AnalyzeArgument(ConcurrentBag<IFieldSymbol> ineligibleFields, SyntaxNodeAnalysisContext context)
        {
            // An argument will disqualify a field if that field is used in a ref/out position.  
            // We can't change such field references to be property references in C#.
            var argument = (ArgumentSyntax)context.Node;
            if (argument.RefOrOutKeyword.Kind() == SyntaxKind.None)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(argument.Expression, cancellationToken);
            AddIneligibleField(symbolInfo.Symbol, ineligibleFields);
            foreach (var symbol in symbolInfo.CandidateSymbols)
            {
                AddIneligibleField(symbol, ineligibleFields);
            }
        }

        private static void AddIneligibleField(ISymbol symbol, ConcurrentBag<IFieldSymbol> ineligibleFields)
        {
            var field = symbol as IFieldSymbol;
            if (field != null)
            {
                ineligibleFields.Add(field);
            }
        }

        private void AnalyzeProperty(ConcurrentBag<AnalysisResult> analysisResults, SymbolAnalysisContext symbolContext)
        {
            var property = (IPropertySymbol)symbolContext.Symbol;
            if (property.IsIndexer)
            {
                return;
            }

            // The property can't be virtual.  We don't know if it is overridden somewhere.  If it 
            // is, then calls to it may not actually assign to the field.
            if (property.IsVirtual || property.IsOverride || property.IsSealed)
            {
                return;
            }

            if (property.IsWithEvents)
            {
                return;
            }

            if (property.Parameters.Length > 0)
            {
                return;
            }

            if (property.GetMethod == null)
            {
                return;
            }

            var containingType = property.ContainingType;
            if (containingType == null)
            {
                return;
            }

            var declarations = property.DeclaringSyntaxReferences;
            if (declarations.Length != 1)
            {
                return;
            }

            var syntaxReference = declarations[0];
            var propertyDeclaration = declarations[0].GetSyntax(symbolContext.CancellationToken) as PropertyDeclarationSyntax;
            if (propertyDeclaration == null)
            {
                return;
            }

            // Need at least a getter.
            var getAccessor = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(d => d.Kind() == SyntaxKind.GetAccessorDeclaration);
            if (getAccessor == null)
            {
                return;
            }

            // A setter is optional though.
            var setAccessor = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(d => d.Kind() == SyntaxKind.SetAccessorDeclaration);

            var semanticModel = symbolContext.Compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            IFieldSymbol getterField = null, setterField = null;
            if (!CheckGetter(semanticModel, containingType, getAccessor, ref getterField))
            {
                return;
            }

            // Property and field have to agree on type.
            if (!property.Type.Equals(getterField.Type))
            {
                return;
            }

            // Don't want to remove constants.
            if (getterField.IsConst)
            {
                return;
            }

            if (getterField.DeclaringSyntaxReferences.Length != 1)
            {
                return;
            }

            // Field and property should match in static-ness
            if (getterField.IsStatic != property.IsStatic)
            {
                return;
            }

            if (setAccessor != null && !CheckSetter(semanticModel, containingType, setAccessor, ref setterField))
            {
                return;
            }

            if (getterField != null && setterField != null)
            {
                // If there is a getter and a setter, they both need to agree on which field they are 
                // writing to.
                if (getterField != setterField)
                {
                    return;
                }
            }

            var fieldReference = getterField.DeclaringSyntaxReferences[0];
            var variableDeclarator = fieldReference.GetSyntax(symbolContext.CancellationToken) as VariableDeclaratorSyntax;
            if (variableDeclarator == null)
            {
                return;
            }

            var fieldDeclaration = variableDeclarator?.Parent?.Parent as FieldDeclarationSyntax;
            if (fieldDeclaration == null)
            {
                return;
            }

            // Can't remove the field if it has attributes on it.
            if (fieldDeclaration.AttributeLists.Count > 0)
            {
                return;
            }

            // Looks like a viable property/field to convert into an auto property.
            analysisResults.Add(new AnalysisResult(property, getterField, propertyDeclaration, fieldDeclaration, variableDeclarator,
                property.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        private bool CheckSetter(SemanticModel semanticModel, ISymbol containingType, AccessorDeclarationSyntax setAccessor, ref IFieldSymbol setterField)
        {
            // Setter has to be of the form:
            //
            //      set { field = value; } or
            //      set { this.field = value; }

            if (setAccessor.Body != null &&
                setAccessor.Body.Statements.Count == 1)
            {
                var firstStatement = setAccessor.Body.Statements[0];
                if (firstStatement.Kind() == SyntaxKind.ExpressionStatement)
                {
                    var expressionStatement = (ExpressionStatementSyntax)firstStatement;
                    if (expressionStatement.Expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                    {
                        var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
                        if (assignmentExpression.Right.Kind() == SyntaxKind.IdentifierName &&
                            ((IdentifierNameSyntax)assignmentExpression.Right).Identifier.ValueText == "value")
                        {
                            return CheckFieldAccessExpression(semanticModel, containingType, assignmentExpression.Left, ref setterField);
                        }
                    }
                }
            }

            // TODO(cyrusn): Update this for arrow body methods.

            return false;
        }

        private bool CheckGetter(SemanticModel semanticModel, ISymbol containingType, AccessorDeclarationSyntax getAccessor, ref IFieldSymbol getterField)
        {
            // Getter has to be of the form:
            //
            //      get { return field; } or
            //      get { return this.field; }

            if (getAccessor.Body != null &&
                getAccessor.Body.Statements.Count == 1)
            {
                var firstStatement = getAccessor.Body.Statements[0];
                if (firstStatement.Kind() == SyntaxKind.ReturnStatement)
                {
                    return CheckFieldAccessExpression(semanticModel, containingType, ((ReturnStatementSyntax)firstStatement).Expression, ref getterField);
                }
            }

            // TODO(cyrusn): Update this for arrow body methods.

            return false;
        }

        private bool CheckFieldAccessExpression(SemanticModel semanticModel, ISymbol containingType, ExpressionSyntax expression, ref IFieldSymbol field)
        {
            if (expression == null)
            {
                return false;
            }

            if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccessExpression = (MemberAccessExpressionSyntax)expression;
                if (memberAccessExpression.Expression.Kind() != SyntaxKind.ThisExpression ||
                    memberAccessExpression.Name.Kind() != SyntaxKind.IdentifierName)
                {
                    return false;
                }
            }
            else if (expression.Kind() != SyntaxKind.IdentifierName)
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (symbolInfo.Symbol == null || symbolInfo.Symbol.Kind != SymbolKind.Field || symbolInfo.Symbol.ContainingType != containingType)
            {
                return false;
            }

            field = (IFieldSymbol)symbolInfo.Symbol;
            return true;
        }

        private void Process(
            ConcurrentBag<AnalysisResult> analysisResults,
            ConcurrentBag<IFieldSymbol> ineligibleFields,
            CompilationAnalysisContext compilationContext)
        {
            var ineligibleFieldsSet = new HashSet<IFieldSymbol>(ineligibleFields);
            foreach (var result in analysisResults)
            {
                var field = result.Field;
                if (ineligibleFieldsSet.Contains(field))
                {
                    continue;
                }

                Process(result, compilationContext);
            }
        }

        private void Process(AnalysisResult result, CompilationAnalysisContext compilationContext)
        {
            var propertyDeclaration = result.PropertyDeclaration;
            var fieldDeclaration = result.FieldDeclaration;
            var variableDeclarator = result.VariableDeclarator;

            var fadeLocation = fieldDeclaration.Declaration.Variables.Count == 1
                ? fieldDeclaration
                : (SyntaxNode)variableDeclarator;

            var properties = ImmutableDictionary<string, string>.Empty.Add(nameof(result.SymbolEquivalenceKey), result.SymbolEquivalenceKey);

            // Fade out the field/variable we are going to remove.
            var diagnostic1 = Diagnostic.Create(FadedTokenDescriptor, fadeLocation.GetLocation());
            compilationContext.ReportDiagnostic(diagnostic1);

            // Now add diagnostics to both the field and the property saying we can convert it to 
            // an auto property.  For each diagnostic store both location so we can easily retrieve
            // them when performing the code fix.
            IEnumerable<Location> additionalLocations = new Location[] { propertyDeclaration.GetLocation(), fadeLocation.GetLocation() };

            var diagnostic2 = Diagnostic.Create(Descriptor, propertyDeclaration.GetLocation(), additionalLocations, properties);
            compilationContext.ReportDiagnostic(diagnostic2);

            var diagnostic3 = Diagnostic.Create(Descriptor, fadeLocation.GetLocation(), additionalLocations, properties);
            compilationContext.ReportDiagnostic(diagnostic3);
        }
    }

    internal class AnalysisResult
    {
        public readonly IPropertySymbol Property;
        public readonly IFieldSymbol Field;
        public readonly PropertyDeclarationSyntax PropertyDeclaration;
        public readonly FieldDeclarationSyntax FieldDeclaration;
        public readonly VariableDeclaratorSyntax VariableDeclarator;
        public readonly string SymbolEquivalenceKey;

        public AnalysisResult(
            IPropertySymbol property,
            IFieldSymbol field,
            PropertyDeclarationSyntax propertyDeclaration,
            FieldDeclarationSyntax fieldDeclaration,
            VariableDeclaratorSyntax variableDeclarator,
            string symbolEquivalenceKey)
        {
            Property = property;
            Field = field;
            PropertyDeclaration = propertyDeclaration;
            FieldDeclaration = fieldDeclaration;
            VariableDeclarator = variableDeclarator;
            SymbolEquivalenceKey = symbolEquivalenceKey;
        }
    }
}