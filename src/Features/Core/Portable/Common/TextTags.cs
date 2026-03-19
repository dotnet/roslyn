// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// The set of well known text tags used for the <see cref="TaggedText.Tag"/> property.
/// These tags influence the presentation of text.
/// </summary>
public static class TextTags
{
    public const string Alias = nameof(Alias);
    public const string Assembly = nameof(Assembly);
    public const string Class = nameof(Class);
    public const string Delegate = nameof(Delegate);
    public const string Enum = nameof(Enum);
    public const string ErrorType = nameof(ErrorType);
    public const string Event = nameof(Event);
    public const string Field = nameof(Field);
    public const string Interface = nameof(Interface);
    public const string Keyword = nameof(Keyword);
    public const string Label = nameof(Label);
    public const string LineBreak = nameof(LineBreak);
    public const string NumericLiteral = nameof(NumericLiteral);
    public const string StringLiteral = nameof(StringLiteral);
    public const string Local = nameof(Local);
    public const string Method = nameof(Method);
    public const string Module = nameof(Module);
    public const string Namespace = nameof(Namespace);
    public const string Operator = nameof(Operator);
    public const string Parameter = nameof(Parameter);
    public const string Property = nameof(Property);
    public const string Punctuation = nameof(Punctuation);
    public const string Space = nameof(Space);
    public const string Struct = nameof(Struct);
    public const string AnonymousTypeIndicator = nameof(AnonymousTypeIndicator);
    public const string Text = nameof(Text);
    public const string TypeParameter = nameof(TypeParameter);
    public const string RangeVariable = nameof(RangeVariable);
    public const string EnumMember = nameof(EnumMember);
    public const string ExtensionMethod = nameof(ExtensionMethod);
    public const string Constant = nameof(Constant);
    public const string Record = nameof(Record);
    public const string RecordStruct = nameof(RecordStruct);

    /// <summary>
    /// Indicates the start of a text container. The elements after <see cref="ContainerStart"/> through (but not
    /// including) the matching <see cref="ContainerEnd"/> are rendered in a rectangular block which is positioned
    /// as an inline element relative to surrounding elements. The text of the <see cref="ContainerStart"/> element
    /// itself precedes the content of the container, and is typically a bullet or number header for an item in a
    /// list.
    /// </summary>
    internal const string ContainerStart = nameof(ContainerStart);

    /// <summary>
    /// Indicates the end of a text container. See <see cref="ContainerStart"/>.
    /// </summary>
    internal const string ContainerEnd = nameof(ContainerEnd);

    /// <summary>
    /// Indicates the start of a code block.  The elements after <see cref="CodeBlockStart"/>
    /// through (but not including) the matching <see cref="CodeBlockEnd"/> are rendered as
    /// a codeblock in LSP markup.
    /// </summary>
    internal const string CodeBlockStart = nameof(CodeBlockStart);
    internal const string CodeBlockEnd = nameof(CodeBlockEnd);
}
