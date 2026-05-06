// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPCSharpRazorKeywordCompletionItemProvider : CSharpRazorKeywordCompletionItemProvider;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPDirectiveCompletionItemProvider : DirectiveCompletionItemProvider;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPDirectiveAttributeCompletionItemProvider : DirectiveAttributeCompletionItemProvider;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPDirectiveAttributeEventParameterCompletionItemProvider : DirectiveAttributeEventParameterCompletionItemProvider;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPDirectiveAttributeTransitionCompletionItemProvider(IClientCapabilitiesService clientCapabilitiesService)
    : DirectiveAttributeTransitionCompletionItemProvider(clientCapabilitiesService);

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPMarkupTransitionCompletionItemProvider : MarkupTransitionCompletionItemProvider;

[Export(typeof(IRazorCompletionItemProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPTagHelperCompletionProvider(ITagHelperCompletionService tagHelperCompletionService)
    : TagHelperCompletionProvider(tagHelperCompletionService);

[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class OOPBlazorDataAttributeCompletionItemProvider : BlazorDataAttributeCompletionItemProvider;
