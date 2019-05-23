using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.LanguageServices.CodeLens;

namespace Microsoft.CodeAnalysis.UnitTesting.ExternalAccess
{
    internal class UnitTestingReferencesService
    {
        private static readonly IEnumerable<(string MethodFullyQualifedName, string MethodFilePath, string MethodOutputFilePath)> Empty =
            Enumerable.Empty<(string MethodFullyQualifedName, string MethodFilePath, string MethodOutputFilePath)>();

        internal static async Task<IEnumerable<(string MethodFullyQualifedName, string MethodFilePath, string MethodOutputFilePath)>> GetCallerMethodsAsync(
            IAsyncCodeLensDataPointProvider provider,
            ICodeLensCallbackService callbackService,
            CodeLensDescriptor descriptor,
            CodeLensDescriptorContext descriptorContext,
            CancellationToken cancellationToken)
        {
            var callerMethods = await callbackService.InvokeAsync<IEnumerable<ReferenceMethodDescriptor>>(
                provider,
                nameof(ICodeLensContext.FindReferenceMethodsAsync),
                new object[] { descriptor, descriptorContext },
                cancellationToken).ConfigureAwait(false);

            if (callerMethods == null || !callerMethods.Any())
            {
                return Empty;
            }

            return callerMethods.Select(m => (
                MethodFullyQualifiedName: m.FullName,
                MethodFilePath: m.FilePath,
                MethodOutputFilePath: m.OutputFilePath));
        }
    }
}
