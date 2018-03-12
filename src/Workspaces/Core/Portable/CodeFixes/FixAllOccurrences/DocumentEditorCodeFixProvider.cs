using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    public abstract class DocumentEditorCodeFixProvider : CodeFixProvider
    {
        public sealed override FixAllProvider GetFixAllProvider() => this.FixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context) => this.RegisterCodeFixesAsync(new DocumentEditorCodeFixContext(context));

        protected virtual DocumentEditorFixAllProvider FixAllProvider() => DocumentEditorFixAllProvider.CurrentDocument;

        protected abstract Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context);
    }
}
