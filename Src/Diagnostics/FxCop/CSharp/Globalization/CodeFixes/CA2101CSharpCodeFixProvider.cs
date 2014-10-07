// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Interoperability;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Globalization
{
    [ExportCodeFixProvider(PInvokeDiagnosticAnalyzer.CA2101, LanguageNames.CSharp), Shared]
    public class CA2101CSharpCodeFixProvider : CA2101CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var charSetType = WellKnownTypes.CharSet(model.Compilation);
            var dllImportType = WellKnownTypes.DllImportAttribute(model.Compilation);
            var marshalAsType = WellKnownTypes.MarshalAsAttribute(model.Compilation);
            var unmanagedType = WellKnownTypes.UnmanagedType(model.Compilation);
            if (charSetType == null || dllImportType == null || marshalAsType == null || unmanagedType == null)
            {
                return Task.FromResult(document);
            }

            var syntaxFactoryService = document.Project.LanguageServices.GetService<SyntaxGenerator>();

            // return the unchanged root if no fix is available
            var newRoot = root;
            if (nodeToFix.CSharpKind() == SyntaxKind.Attribute)
            {
                // could be either a [DllImport] or [MarshalAs] attribute
                var attribute = (AttributeSyntax)nodeToFix;
                var attributeType = model.GetSymbolInfo(attribute).Symbol;
                var arguments = attribute.ArgumentList.Arguments;
                if (dllImportType.Equals(attributeType.ContainingType))
                {
                    // [DllImport] attribute, add or replace CharSet named parameter
                    var argumentValue = CreateCharSetArgument(syntaxFactoryService, charSetType).WithAdditionalAnnotations(Formatter.Annotation);
                    var namedParameter = arguments.FirstOrDefault(arg => arg.NameEquals != null && arg.NameEquals.Name.Identifier.Text == CharSetText);
                    if (namedParameter == null)
                    {
                        // add the parameter
                        namedParameter = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(CharSetText), null, (ExpressionSyntax)argumentValue)
                            .WithAdditionalAnnotations(Formatter.Annotation);
                        var newArguments = arguments.Add(namedParameter);
                        var newArgumentList = attribute.ArgumentList.WithArguments(newArguments);
                        newRoot = root.ReplaceNode(attribute.ArgumentList, newArgumentList);
                    }
                    else
                    {
                        // replace the parameter
                        var newNamedParameter = namedParameter.WithExpression((ExpressionSyntax)argumentValue);
                        newRoot = root.ReplaceNode(namedParameter, newNamedParameter);
                    }
                }
                else if (marshalAsType.Equals(attributeType.ContainingType) && arguments.Count == 1)
                {
                    // [MarshalAs] attribute, replace the only argument
                    var newExpression = CreateMarshalAsArgument(syntaxFactoryService, unmanagedType)
                        .WithLeadingTrivia(arguments[0].GetLeadingTrivia())
                        .WithTrailingTrivia(arguments[0].GetTrailingTrivia());
                    var newArgument = arguments[0].WithExpression((ExpressionSyntax)newExpression);
                    newRoot = root.ReplaceNode(arguments[0], newArgument);
                }
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
