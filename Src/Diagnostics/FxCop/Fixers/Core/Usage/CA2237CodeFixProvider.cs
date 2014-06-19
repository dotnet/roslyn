using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [ExportCodeFixProvider("CA2237 CodeFix provider", LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA2237CodeFixProvider : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(SerializationRulesDiagnosticAnalyzer.RuleCA2237Id);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.AddSerializableAttribute;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            var attr = CodeGenerationSymbolFactory.CreateAttributeData(WellKnownTypes.SerializableAttribute(model.Compilation));
            var newNode = CodeGenerator.AddAttributes(nodeToFix, document.Project.Solution.Workspace, SpecializedCollections.SingletonEnumerable(attr)).WithAdditionalAnnotations(Formatting.Formatter.Annotation);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(nodeToFix, newNode)));
        }
    }
}
