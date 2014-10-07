namespace Roslyn.Services
{
    public interface ISolutionEdit
    {
        IDocumentEdit GetDocumentEdit(DocumentId documentId);
        void Apply();

        ISolution Solution { get; }
    }
}