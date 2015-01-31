// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementInterface
{
    [ExportDiagnosticProvider(PredefinedDiagnosticProviderNames.ImplementInterface, LanguageNames.CSharp)]
    internal sealed class ImplementInterfaceDiagnosticProvider : DocumentDiagnosticProvider
    {
        internal const string DiagnosticId = "ImplementInterface";
        internal static readonly DiagnosticDescriptor DiagnosticMD = new DiagnosticDescriptor(DiagnosticId,
                                                                                              DiagnosticKind.Hidden,
                                                                                              CSharpFeaturesResources.ImplementInterface,
                                                                                              CSharpFeaturesResources.ImplementInterface,
                                                                                              "Internal",
                                                                                              DiagnosticSeverity.None);
        internal const string CS0535 = "CS0535"; // 'Program' does not implement interface member 'System.Collections.IEnumerable.GetEnumerator()'
        internal const string CS0737 = "CS0737"; // 'Class' does not implement interface member 'IInterface.M()'. 'Class.M()' cannot implement an interface member because it is not public.
        internal const string CS0738 = "CS0738"; // 'C' does not implement interface member 'I.Method1()'. 'B.Method1()' cannot implement 'I.Method1()' because it does not have the matching return type of 'void'.

        public override IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
        {
            return SpecializedCollections.SingletonEnumerable(DiagnosticMD);
        }

        protected override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (!document.IsOpen())
            {
                return null;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = model.SyntaxTree.GetRoot(cancellationToken);

            var infos = model.GetDeclarationDiagnostics(cancellationToken).Where(d =>
                d.Id == CS0535 ||
                d.Id == CS0737 ||
                d.Id == CS0738).GroupBy(d => d.Location.SourceSpan).Select(g => g.First());

            var service = document.GetLanguageService<IImplementInterfaceService>();

            var diagnostics = new List<Diagnostic>();

            foreach (var error in infos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var espan = error.Location.SourceSpan;
                var token = root.FindToken(espan.Start);
                if (!token.Span.IntersectsWith(espan))
                {
                    continue;
                }

                var typeNode = token.Parent as TypeDeclarationSyntax;
                if (typeNode == null)
                {
                    continue;
                }

                IEnumerable<TypeSyntax> baseListTypes = typeNode.GetAllBaseListTypes(model, cancellationToken);

                foreach (var node in baseListTypes)
                {
                    if (service.GetCodeActions(
                        document,
                        model,
                        node,
                        cancellationToken).Any())
                    {
                        diagnostics.Add(CreateUserDiagnostic(document, node, cancellationToken));
                    }
                }
            }

            return diagnostics;
        }

        private Diagnostic CreateUserDiagnostic(Document document, TypeSyntax node, CancellationToken cancellationToken)
        {
            var span = node.Span;
            var tree = node.SyntaxTree;

            return Diagnostic.Create(DiagnosticMD, tree.GetLocation(span));
        }
    }
}
