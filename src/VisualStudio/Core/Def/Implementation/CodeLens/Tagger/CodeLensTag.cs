// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.CodeLens;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger
{
    internal partial class CodeLensTag : ICodeLensTag2, ICodeLensDescriptorContextProvider
    {
        private static readonly int VisualStudioProcessId = Process.GetCurrentProcess().Id;

        private static readonly SymbolDisplayFormat MethodDisplayFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        private readonly CodeLensDescriptor _descriptor;

        public CodeLensTag(CodeLensDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public event EventHandler Disconnected { add { } remove { } }

        public ICodeLensDescriptor Descriptor => _descriptor;

        public ICodeLensDescriptorContextProvider DescriptorContextProvider => this;

        public async Task<CodeLensDescriptorContext?> GetCurrentContextAsync()
        {
            var cancellationToken = CancellationToken.None;
            var semanticModel = await _descriptor.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var fullyQualifiedName = GetFullyQualifiedName(semanticModel, _descriptor.SyntaxNode, cancellationToken);

            var documentId = _descriptor.Document.Id;
            var lineSpan = _descriptor.LineSpan;

            return new CodeLensDescriptorContext(
                applicableSpan: _descriptor.SyntaxNode.Span.ToSpan(),
                properties: new Dictionary<object, object?>()
                {
                    { "VisualStudioProcessId", VisualStudioProcessId },
                    { "OutputFilePath", _descriptor.OutputFilePath },
                    { "FullyQualifiedName", fullyQualifiedName },
                    { "StartLine", lineSpan.StartLinePosition.Line },
                    { "StartColumn", lineSpan.StartLinePosition.Character },
                    { "RoslynDocumentIdGuid", documentId.Id.ToString() },
                    { "RoslynProjectIdGuid", documentId.ProjectId.Id.ToString() },
                });
        }

        private string GetFullyQualifiedName(SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);

            if (symbol == null)
            {
                return string.Empty;
            }

            var parts = symbol.ToDisplayParts(MethodDisplayFormat);

            var previousWasType = false;
            var builder = new StringBuilder();
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                if (previousWasType &&
                    part.Kind == SymbolDisplayPartKind.Punctuation &&
                    index < parts.Length - 1)
                {
                    switch (parts[index + 1].Kind)
                    {
                        case SymbolDisplayPartKind.ClassName:
                        case SymbolDisplayPartKind.DelegateName:
                        case SymbolDisplayPartKind.EnumName:
                        case SymbolDisplayPartKind.ErrorTypeName:
                        case SymbolDisplayPartKind.InterfaceName:
                        case SymbolDisplayPartKind.StructName:
                            builder.Append('+');
                            break;

                        default:
                            builder.Append(part);
                            break;
                    }
                }
                else
                {
                    builder.Append(part);
                }

                previousWasType = part.Kind == SymbolDisplayPartKind.ClassName ||
                                  part.Kind == SymbolDisplayPartKind.InterfaceName ||
                                  part.Kind == SymbolDisplayPartKind.StructName;
            }

            return builder.ToString();
        }
    }
}
