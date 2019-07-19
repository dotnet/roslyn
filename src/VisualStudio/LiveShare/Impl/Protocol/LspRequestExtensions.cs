using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    public static class LspRequestExtensions
    {
        public static LS.LspRequest<TIn, TOut> ToLSRequest<TIn, TOut>(this LSP.LspRequest<TIn, TOut> lspRequest)
        {

            return new LS.LspRequest<TIn, TOut>(lspRequest.Name);
        }

    }
}
