namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    /// <summary>
    /// Class which represents a document reference
    /// </summary>
    [DataContract]
    public class TextDocumentParams
    {
        /// <summary>
        /// Gets or sets the value which identifies the external document.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}
