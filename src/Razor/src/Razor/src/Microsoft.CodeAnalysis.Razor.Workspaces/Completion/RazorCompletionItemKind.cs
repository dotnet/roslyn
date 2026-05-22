// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal enum RazorCompletionItemKind
{
    CSharpRazorKeyword,
    Directive,
    DirectiveAttribute,
    DirectiveAttributeParameter,
    DirectiveAttributeParameterEventValue,
    MarkupTransition,
    TagHelperElement,
    TagHelperAttribute,
    Attribute,
}
