using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.CodeGeneration
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal abstract class CodeDefinition : CommonSyntaxNode
    {
        protected static ConditionalWeakTable<CodeDefinition, SyntaxAnnotation[]> annotationsTable =
            new ConditionalWeakTable<CodeDefinition, SyntaxAnnotation[]>();

        public abstract void Accept(ICodeDefinitionVisitor visitor);
        public abstract T Accept<T>(ICodeDefinitionVisitor<T> visitor);
        public abstract TResult Accept<TArgument, TResult>(ICodeDefinitionVisitor<TArgument, TResult> visitor, TArgument argument);

        protected abstract CodeDefinition Clone();

        internal SyntaxAnnotation[] GetAnnotations()
        {
            SyntaxAnnotation[] annotations;
            annotationsTable.TryGetValue(this, out annotations);
            return annotations ?? SpecializedCollections.EmptyArray<SyntaxAnnotation>();
        }

        protected internal virtual new string DebuggerDisplay
        {
            get { return GetType().Name; }
        }

        protected override CommonSyntaxNode WithAdditionalAnnotationsCore(SyntaxAnnotation[] annotations)
        {
            return annotations.IsNullOrEmpty()
                ? this
                : AddAnnotationsTo(this, this.Clone(), annotations);
        }

        private CodeDefinition AddAnnotationsTo(
            CodeDefinition originalDefinition, CodeDefinition newDefinition, SyntaxAnnotation[] annotations)
        {
            SyntaxAnnotation[] originalAnnotations;
            annotationsTable.TryGetValue(originalDefinition, out originalAnnotations);

            annotations = SyntaxAnnotationExtensions.CombineAnnotations(originalAnnotations, annotations);
            annotationsTable.Add(newDefinition, annotations);

            return newDefinition;
        }

        #region CommonSyntaxNode

        public override string Language
        {
            get { throw new System.NotImplementedException(); }
        }

        public override TextSpan Span
        {
            get { throw new System.NotImplementedException(); }
        }

        public override TextSpan FullSpan
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string GetText()
        {
            throw new System.NotImplementedException();
        }

        public override string GetFullText()
        {
            throw new System.NotImplementedException();
        }

        public override void WriteTo(System.IO.TextWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsMissing
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool IsStructuredTrivia
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool HasDirectives
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool HasDiagnostics
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool HasLeadingTrivia
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool HasTrailingTrivia
        {
            get { throw new System.NotImplementedException(); }
        }

        protected override int KindCore
        {
            get { throw new System.NotImplementedException(); }
        }

        protected override CommonChildSyntaxList ChildNodesAndTokensCore()
        {
            throw new System.NotImplementedException();
        }

        protected override bool EquivalentToCore(CommonSyntaxNode other)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxNode ParentCore
        {
            get { throw new System.NotImplementedException(); }
        }

        protected override CommonSyntaxTree SyntaxTreeCore
        {
            get { throw new System.NotImplementedException(); }
        }

        protected override CommonSyntaxToken FindTokenCore(int position, bool findInsideTrivia)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxToken FindTokenCore(int position, System.Func<CommonSyntaxTrivia, bool> stepInto)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxTrivia FindTriviaCore(int position, bool findInsideTrivia)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxToken GetFirstTokenCore()
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxToken GetFirstTokenCore(System.Func<CommonSyntaxToken, bool> predicate, System.Func<CommonSyntaxTrivia, bool> stepInto)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxToken GetLastTokenCore()
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxToken GetLastTokenCore(System.Func<CommonSyntaxToken, bool> predicate, System.Func<CommonSyntaxTrivia, bool> stepInto)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxNode ReplaceNodesCore(System.Collections.Generic.IEnumerable<CommonSyntaxNode> oldNodes, System.Func<CommonSyntaxNode, CommonSyntaxNode, CommonSyntaxNode> computeReplacementNode)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxNode ReplaceNodeCore(CommonSyntaxNode oldNode, CommonSyntaxNode newNode)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxNode ReplaceTokensCore(System.Collections.Generic.IEnumerable<CommonSyntaxToken> oldTokens, System.Func<CommonSyntaxToken, CommonSyntaxToken, CommonSyntaxToken> computeReplacementToken)
        {
            throw new System.NotImplementedException();
        }

        protected override CommonSyntaxNode ReplaceTokenCore(CommonSyntaxToken oldToken, CommonSyntaxToken newToken)
        {
            throw new System.NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<CommonSyntaxNodeOrToken> GetAnnotatedNodesAndTokensCore(System.Type annotationType)
        {
            throw new System.NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<CommonSyntaxNodeOrToken> GetAnnotatedNodesAndTokensCore(SyntaxAnnotation annotation)
        {
            throw new System.NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<CommonSyntaxTrivia> GetAnnotatedTriviaCore(System.Type annotationType)
        {
            throw new System.NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<CommonSyntaxTrivia> GetAnnotatedTriviaCore(SyntaxAnnotation annotation)
        {
            throw new System.NotImplementedException();
        }

        public override System.Collections.Generic.IEnumerable<SyntaxAnnotation> GetAnnotations(System.Type annotationType)
        {
            throw new System.NotImplementedException();
        }

        public override bool HasAnnotations(System.Type annotationType)
        {
            throw new System.NotImplementedException();
        }

        public override bool HasAnnotation(SyntaxAnnotation annotation)
        {
            throw new System.NotImplementedException();
        }

        public override bool HasAnyAnnotations
        {
            get { throw new System.NotImplementedException(); }
        }

        #endregion
    }
}