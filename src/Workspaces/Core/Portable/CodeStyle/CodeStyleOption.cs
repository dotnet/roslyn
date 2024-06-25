// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: CodeStyleOption<T>

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <summary>
/// Public representation of a code style option value. Should only be used for public API.
/// Internally the value is represented by <see cref="ICodeStyleOption2"/>.
/// </summary>
internal interface ICodeStyleOption
{
    XElement ToXElement();
    object? Value { get; }
    NotificationOption2 Notification { get; }
    ICodeStyleOption WithValue(object value);
    ICodeStyleOption WithNotification(NotificationOption2 notification);
}

/// <inheritdoc cref="CodeStyleOption2{T}"/>
public sealed class CodeStyleOption<T> : ICodeStyleOption, IEquatable<CodeStyleOption<T>>
{
    private readonly CodeStyleOption2<T> _codeStyleOptionImpl;
    public static CodeStyleOption<T> Default => new(default!, NotificationOption.Silent);

    internal CodeStyleOption(CodeStyleOption2<T> codeStyleOptionImpl)
        => _codeStyleOptionImpl = codeStyleOptionImpl;

    public CodeStyleOption(T value, NotificationOption notification)
        : this(new CodeStyleOption2<T>(value, new NotificationOption2(notification.Severity, IsExplicitlySpecified: false)))
    {
    }

    public T Value
    {
        get => _codeStyleOptionImpl.Value;

        [Obsolete("Modifying a CodeStyleOption<T> is not supported.", error: true)]
        set => throw new InvalidOperationException();
    }

    object? ICodeStyleOption.Value => this.Value;
    NotificationOption2 ICodeStyleOption.Notification => _codeStyleOptionImpl.Notification;
    ICodeStyleOption ICodeStyleOption.WithValue(object value) => new CodeStyleOption<T>((T)value, Notification);
    ICodeStyleOption ICodeStyleOption.WithNotification(NotificationOption2 notification) => new CodeStyleOption<T>(Value, (NotificationOption)notification);

    public NotificationOption Notification
    {
        get => (NotificationOption)_codeStyleOptionImpl.Notification;

        [Obsolete("Modifying a CodeStyleOption<T> is not supported.", error: true)]
        set => throw new InvalidOperationException();
    }

    internal CodeStyleOption2<T> UnderlyingOption => _codeStyleOptionImpl;

    public XElement ToXElement() => _codeStyleOptionImpl.ToXElement();

    public static CodeStyleOption<T> FromXElement(XElement element)
        => new(CodeStyleOption2<T>.FromXElement(element));

    public bool Equals(CodeStyleOption<T>? other)
        => _codeStyleOptionImpl.Equals(other?._codeStyleOptionImpl);

    public override bool Equals(object? obj)
        => obj is CodeStyleOption<T> option &&
           Equals(option);

    public override int GetHashCode()
        => _codeStyleOptionImpl.GetHashCode();
}
