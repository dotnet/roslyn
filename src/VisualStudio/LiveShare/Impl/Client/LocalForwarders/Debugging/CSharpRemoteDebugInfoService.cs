// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportLanguageServiceFactory(typeof(ILanguageDebugInfoService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspDebugInfoServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpRemoteDebugInfoService(languageServices);
        }
    }

    internal class CSharpRemoteDebugInfoService : ILanguageDebugInfoService
    {
        private readonly HostLanguageServices languageServices;

        public CSharpRemoteDebugInfoService(HostLanguageServices languageServices)
        {
            this.languageServices = languageServices;
        }

        public async Task<DebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root == null)
                {
                    return default;
                }

                var token = root.FindToken(position);

                // If the given token is an identifier then return it's span, otherwise if it's part of an expression, return the entire expression's span.
                var expression = token.Parent as ExpressionSyntax;
                if (expression == null)
                {
                    return token.IsKind(SyntaxKind.IdentifierToken)
                        ? new DebugDataTipInfo(token.Span, text: null)
                        : default;
                }

                return new DebugDataTipInfo(expression.Span, text: null);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public Task<DebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            return this.languageServices.GetOriginalLanguageService<ILanguageDebugInfoService>().GetLocationInfoAsync(document, position, cancellationToken);
        }
    }
}
