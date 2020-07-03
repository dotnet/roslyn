// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <inheritdoc cref="CodeStyleOption2{T}"/>
    public class CodeStyleOption<T> : ICodeStyleOption, IEquatable<CodeStyleOption<T>>
    {
        static CodeStyleOption()
        {
            ObjectBinder.RegisterTypeReader(typeof(CodeStyleOption<T>), ReadFrom);
        }

        private readonly CodeStyleOption2<T> _codeStyleOptionImpl;
        public static CodeStyleOption<T> Default => new CodeStyleOption<T>(default, NotificationOption.Silent);

        internal CodeStyleOption(CodeStyleOption2<T> codeStyleOptionImpl)
            => _codeStyleOptionImpl = codeStyleOptionImpl;

        public CodeStyleOption(T value, NotificationOption notification)
            : this(new CodeStyleOption2<T>(value, (NotificationOption2)notification))
        {
        }

        public T Value
        {
            get => _codeStyleOptionImpl.Value;
            set => _codeStyleOptionImpl.Value = value;
        }

        bool IObjectWritable.ShouldReuseInSerialization => _codeStyleOptionImpl.ShouldReuseInSerialization;
        object ICodeStyleOption.Value => this.Value;
        NotificationOption2 ICodeStyleOption.Notification => _codeStyleOptionImpl.Notification;
        ICodeStyleOption ICodeStyleOption.WithValue(object value) => new CodeStyleOption<T>((T)value, Notification);
        ICodeStyleOption ICodeStyleOption.WithNotification(NotificationOption2 notification) => new CodeStyleOption<T>(Value, (NotificationOption)notification);
        ICodeStyleOption ICodeStyleOption.AsCodeStyleOption<TCodeStyleOption>()
            => this is TCodeStyleOption ? this : (ICodeStyleOption)_codeStyleOptionImpl;
        ICodeStyleOption ICodeStyleOption.AsPublicCodeStyleOption() => this;

        public NotificationOption Notification
        {
            get => (NotificationOption)_codeStyleOptionImpl.Notification;
            set => _codeStyleOptionImpl.Notification = (NotificationOption2)(value ?? throw new ArgumentNullException(nameof(value)));
        }

        internal CodeStyleOption2<T> UnderlyingOption => _codeStyleOptionImpl;

        public XElement ToXElement() => _codeStyleOptionImpl.ToXElement();

        public static CodeStyleOption<T> FromXElement(XElement element)
            => new CodeStyleOption<T>(CodeStyleOption2<T>.FromXElement(element));

        void IObjectWritable.WriteTo(ObjectWriter writer)
            => _codeStyleOptionImpl.WriteTo(writer);

        internal static CodeStyleOption<object> ReadFrom(ObjectReader reader)
            => new CodeStyleOption<object>(CodeStyleOption2<T>.ReadFrom(reader));

        public bool Equals(CodeStyleOption<T> other)
            => _codeStyleOptionImpl.Equals(other?._codeStyleOptionImpl);

        public override bool Equals(object obj)
            => obj is CodeStyleOption<T> option &&
               Equals(option);

        public override int GetHashCode()
            => _codeStyleOptionImpl.GetHashCode();
    }
}
