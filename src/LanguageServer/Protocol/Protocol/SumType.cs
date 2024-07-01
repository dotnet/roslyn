// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json.Serialization;
    using Microsoft.CodeAnalysis.LanguageServer;

    /// <summary>
    /// Struct that may contain a <typeparamref name="T1"/> or a <typeparamref name="T2"/>.
    /// </summary>
    /// <typeparam name="T1">The first type this struct is designed to contain.</typeparam>
    /// <typeparam name="T2">The second type this struct is designed to contain.</typeparam>
    [JsonConverter(typeof(SumConverter))]
    internal struct SumType<T1, T2> : ISumType, IEquatable<SumType<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        static SumType()
        {
            SumTypeUtils.ValidateTypeParameter(typeof(T1));
            SumTypeUtils.ValidateTypeParameter(typeof(T2));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2}"/> struct containing a <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2}"/>.</param>
        public SumType(T1 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2}"/> struct containing a <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2}"/>.</param>
        public SumType(T2 val)
        {
            this.Value = val;
        }

        /// <inheritdoc/>
        public object? Value { get; }

        /// <summary>
        /// Gets the value as the first specified type.
        /// </summary>
        public T1 First => (T1)this;

        /// <summary>
        /// Gets the value as the second specified type.
        /// </summary>
        public T2 Second => (T2)this;

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1"/> with a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2>(T1 val) => new SumType<T1, T2>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1?"/> with a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2>?(T1? val) => val is null ? null : new SumType<T1, T2>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2"/> with a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2>(T2 val) => new SumType<T1, T2>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2?"/> with a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2>?(T2? val) => val is null ? null : new SumType<T1, T2>(val);

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2}"/> to an instance of <typeparamref name="T1"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2}"/> does not contain an instance of <typeparamref name="T1"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T1(SumType<T1, T2> sum) => sum.Value is T1 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2}"/> to an instance of <typeparamref name="T2"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2}"/> does not contain an instance of <typeparamref name="T2"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T2(SumType<T1, T2> sum) => sum.Value is T2 tVal ? tVal : throw new InvalidCastException();

        public static bool operator ==(SumType<T1, T2> left, SumType<T1, T2> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SumType<T1, T2> left, SumType<T1, T2> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Tries to get the value as the first specified type.
        /// </summary>
        /// <param name="value">the value in the specified type.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetFirst([MaybeNullWhen(false)] out T1 value)
        {
            if (this.Value is T1 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the second specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetSecond([MaybeNullWhen(false)] out T2 value)
        {
            if (this.Value is T2 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Runs a delegate corresponding to which type is contained inside this instance.
        /// </summary>
        /// <typeparam name="TResult">The type that all the delegates will return.</typeparam>
        /// <param name="firstMatch">Delegate to handle the case where this instance contains a <typeparamref name="T1"/>.</param>
        /// <param name="secondMatch">Delegate to handle the case where this instance contains a <typeparamref name="T2"/>.</param>
        /// <param name="defaultMatch">
        /// Delegate to handle the case where this instance is uninhabited. If this delegate isn't provided the default
        /// <typeparamref name="TResult"/> will be returned instead.
        /// </param>
        /// <returns>The <typeparamref name="TResult"/> instance created by the delegate corresponding to the current type stored in this instance.</returns>
        public TResult Match<TResult>(Func<T1, TResult> firstMatch, Func<T2, TResult> secondMatch, Func<TResult>? defaultMatch = null)
        {
            if (firstMatch == null)
            {
                throw new ArgumentNullException(nameof(firstMatch));
            }

            if (secondMatch == null)
            {
                throw new ArgumentNullException(nameof(secondMatch));
            }

            if (this.Value is T1 tOne)
            {
                return firstMatch(tOne);
            }

            if (this.Value is T2 tTwo)
            {
                return secondMatch(tTwo);
            }

            if (defaultMatch != null)
            {
                return defaultMatch();
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default(TResult);
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SumType<T1, T2> type && this.Equals(type);
        }

        /// <inheritdoc/>
        public bool Equals(SumType<T1, T2> other)
        {
            return EqualityComparer<object?>.Default.Equals(this.Value, other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return -1937169414 + EqualityComparer<object?>.Default.GetHashCode(this.Value);
        }
    }

    /// <summary>
    /// Struct that may contain a <typeparamref name="T1"/>, a <typeparamref name="T2"/>, or a <typeparamref name="T3"/>.
    /// </summary>
    /// <typeparam name="T1">The first type this struct is designed to contain.</typeparam>
    /// <typeparam name="T2">The second type this struct is designed to contain.</typeparam>
    /// <typeparam name="T3">The third type this struct is designed to contain.</typeparam>
    [JsonConverter(typeof(SumConverter))]
    internal struct SumType<T1, T2, T3> : ISumType, IEquatable<SumType<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        static SumType()
        {
            SumTypeUtils.ValidateTypeParameter(typeof(T1));
            SumTypeUtils.ValidateTypeParameter(typeof(T2));
            SumTypeUtils.ValidateTypeParameter(typeof(T3));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3}"/> struct containing a <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3}"/>.</param>
        public SumType(T1 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3}"/> struct containing a <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3}"/>.</param>
        public SumType(T2 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3}"/> struct containing a <typeparamref name="T3"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3}"/>.</param>
        public SumType(T3 val)
        {
            this.Value = val;
        }

        /// <inheritdoc/>
        public object? Value { get; }

        /// <summary>
        /// Gets the value as the first specified type.
        /// </summary>
        public T1 First => (T1)this;

        /// <summary>
        /// Gets the value as the second specified type.
        /// </summary>
        public T2 Second => (T2)this;

        /// <summary>
        /// Gets the value as the third specified type.
        /// </summary>
        public T3 Third => (T3)this;

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3>(T1 val) => new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1?"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3>?(T1? val) => val is null ? null : new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3>(T2 val) => new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2?"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3>?(T2? val) => val is null ? null : new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T3"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3>(T3 val) => new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T3?"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3>?(T3? val) => val is null ? null : new SumType<T1, T2, T3>(val);

        /// <summary>
        /// Implicitly wraps an instance of <see cref="SumType{T1, T2}"/> with a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to wrap.</param>
        public static implicit operator SumType<T1, T2, T3>(SumType<T1, T2> sum)
            => sum.Match(
                (v) => new SumType<T1, T2, T3>(v),
                (v) => new SumType<T1, T2, T3>(v));

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3}"/> into a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to downcast.</param>
        public static explicit operator SumType<T1, T2>(SumType<T1, T2, T3> sum)
        {
            if (sum.Value is T1 tOne)
            {
                return tOne;
            }

            if (sum.Value is T2 tTwo)
            {
                return tTwo;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3}"/> to an instance of <typeparamref name="T1"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3}"/> does not contain an instance of <typeparamref name="T1"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T1(SumType<T1, T2, T3> sum) => sum.Value is T1 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2}"/> to an instance of <typeparamref name="T2"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3}"/> does not contain an instance of <typeparamref name="T2"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T2(SumType<T1, T2, T3> sum) => sum.Value is T2 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3}"/> to an instance of <typeparamref name="T3"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3}"/> does not contain an instance of <typeparamref name="T3"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T3(SumType<T1, T2, T3> sum) => sum.Value is T3 tVal ? tVal : throw new InvalidCastException();

        public static bool operator ==(SumType<T1, T2, T3> left, SumType<T1, T2, T3> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SumType<T1, T2, T3> left, SumType<T1, T2, T3> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Tries to get the value as the first specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetFirst([MaybeNullWhen(false)] out T1 value)
        {
            if (this.Value is T1 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the second specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetSecond([MaybeNullWhen(false)] out T2 value)
        {
            if (this.Value is T2 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the third specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetThird([MaybeNullWhen(false)] out T3 value)
        {
            if (this.Value is T3 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Runs a delegate corresponding to which type is contained inside this instance.
        /// </summary>
        /// <typeparam name="TResult">The type that all the delegates will return.</typeparam>
        /// <param name="firstMatch">Delegate to handle the case where this instance contains a <typeparamref name="T1"/>.</param>
        /// <param name="secondMatch">Delegate to handle the case where this instance contains a <typeparamref name="T2"/>.</param>
        /// <param name="thirdMatch">Delegate to handle the case where this instance contains a <typeparamref name="T3"/>.</param>
        /// <param name="defaultMatch">
        /// Delegate to handle the case where this instance is uninhabited. If this delegate isn't provided the default
        /// <typeparamref name="TResult"/> will be returned instead.
        /// </param>
        /// <returns>The <typeparamref name="TResult"/> instance created by the delegate corresponding to the current type stored in this instance.</returns>
        public TResult Match<TResult>(Func<T1, TResult> firstMatch, Func<T2, TResult> secondMatch, Func<T3, TResult> thirdMatch, Func<TResult>? defaultMatch = null)
        {
            if (firstMatch == null)
            {
                throw new ArgumentNullException(nameof(firstMatch));
            }

            if (secondMatch == null)
            {
                throw new ArgumentNullException(nameof(secondMatch));
            }

            if (thirdMatch == null)
            {
                throw new ArgumentNullException(nameof(thirdMatch));
            }

            if (this.Value is T1 tOne)
            {
                return firstMatch(tOne);
            }

            if (this.Value is T2 tTwo)
            {
                return secondMatch(tTwo);
            }

            if (this.Value is T3 tThree)
            {
                return thirdMatch(tThree);
            }

            if (defaultMatch != null)
            {
                return defaultMatch();
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default(TResult);
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SumType<T1, T2, T3> type && this.Equals(type);
        }

        /// <inheritdoc/>
        public bool Equals(SumType<T1, T2, T3> other)
        {
            return EqualityComparer<object?>.Default.Equals(this.Value, other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return -1937169414 + EqualityComparer<object?>.Default.GetHashCode(this.Value);
        }
    }

    /// <summary>
    /// Struct that may contain a <typeparamref name="T1"/>, a <typeparamref name="T2"/>, a <typeparamref name="T3"/>, or a <typeparamref name="T4"/>.
    /// </summary>
    /// <typeparam name="T1">The first type this struct is designed to contain.</typeparam>
    /// <typeparam name="T2">The second type this struct is designed to contain.</typeparam>
    /// <typeparam name="T3">The third type this struct is designed to contain.</typeparam>
    /// <typeparam name="T4">The fourth type this struct is designed to contain.</typeparam>
    [JsonConverter(typeof(SumConverter))]
    internal struct SumType<T1, T2, T3, T4> : ISumType, IEquatable<SumType<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        static SumType()
        {
            SumTypeUtils.ValidateTypeParameter(typeof(T1));
            SumTypeUtils.ValidateTypeParameter(typeof(T2));
            SumTypeUtils.ValidateTypeParameter(typeof(T3));
            SumTypeUtils.ValidateTypeParameter(typeof(T4));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3, T4}"/> struct containing a <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3, T4}"/>.</param>
        public SumType(T1 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3, T4}"/> struct containing a <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3, T4}"/>.</param>
        public SumType(T2 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3, T4}"/> struct containing a <typeparamref name="T3"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3, T4}"/>.</param>
        public SumType(T3 val)
        {
            this.Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SumType{T1, T2, T3, T4}"/> struct containing a <typeparamref name="T4"/>.
        /// </summary>
        /// <param name="val">The value to store in the <see cref="SumType{T1, T2, T3, T4}"/>.</param>
        public SumType(T4 val)
        {
            this.Value = val;
        }

        /// <inheritdoc/>
        public object? Value { get; }

        /// <summary>
        /// Gets the value as the first specified type.
        /// </summary>
        public T1 First => (T1)this;

        /// <summary>
        /// Gets the value as the second specified type.
        /// </summary>
        public T2 Second => (T2)this;

        /// <summary>
        /// Gets the value as the third specified type.
        /// </summary>
        public T3 Third => (T3)this;

        /// <summary>
        /// Gets the value as the fourth specified type.
        /// </summary>
        public T4 Fourth => (T4)this;

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(T1 val) => new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T1?"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3, T4>?(T1? val) => val is null ? null : new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(T2 val) => new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T2?"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3, T4>?(T2? val) => val is null ? null : new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T3"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(T3 val) => new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T3?"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3, T4>?(T3? val) => val is null ? null : new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T4"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(T4 val) => new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps a value of type <typeparamref name="T4?"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="val">Value to be wrapped.</param>
        public static implicit operator SumType<T1, T2, T3, T4>?(T4? val) => val is null ? null : new SumType<T1, T2, T3, T4>(val);

        /// <summary>
        /// Implicitly wraps an instance of <see cref="SumType{A, B}"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(SumType<T1, T2> sum)
            => sum.Match(
                (v) => new SumType<T1, T2, T3, T4>(v),
                (v) => new SumType<T1, T2, T3, T4>(v));

        /// <summary>
        /// Implicitly wraps an instance of <see cref="SumType{T1, T2, T3}"/> with a <see cref="SumType{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to wrap.</param>
        public static implicit operator SumType<T1, T2, T3, T4>(SumType<T1, T2, T3> sum)
            => sum.Match(
                (v) => new SumType<T1, T2, T3, T4>(v),
                (v) => new SumType<T1, T2, T3, T4>(v),
                (v) => new SumType<T1, T2, T3, T4>(v));

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> into a <see cref="SumType{T1, T2}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to downcast.</param>
        public static explicit operator SumType<T1, T2>(SumType<T1, T2, T3, T4> sum)
        {
            if (sum.Value is T1 tOne)
            {
                return tOne;
            }

            if (sum.Value is T2 tTwo)
            {
                return tTwo;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> into a <see cref="SumType{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="sum">Sum instance to downcast.</param>
        public static explicit operator SumType<T1, T2, T3>(SumType<T1, T2, T3, T4> sum)
        {
            if (sum.Value is T1 tOne)
            {
                return tOne;
            }

            if (sum.Value is T2 tTwo)
            {
                return tTwo;
            }

            if (sum.Value is T3 tThree)
            {
                return tThree;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> to an instance of <typeparamref name="T1"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3, T4}"/> does not contain an instance of <typeparamref name="T1"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T1(SumType<T1, T2, T3, T4> sum) => sum.Value is T1 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> to an instance of <typeparamref name="T2"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3, T4}"/> does not contain an instance of <typeparamref name="T2"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T2(SumType<T1, T2, T3, T4> sum) => sum.Value is T2 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> to an instance of <typeparamref name="T3"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3, T4}"/> does not contain an instance of <typeparamref name="T3"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T3(SumType<T1, T2, T3, T4> sum) => sum.Value is T3 tVal ? tVal : throw new InvalidCastException();

        /// <summary>
        /// Attempts to cast an instance of <see cref="SumType{T1, T2, T3, T4}"/> to an instance of <typeparamref name="T4"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Thrown if this instance of <see cref="SumType{T1, T2, T3, T4}"/> does not contain an instance of <typeparamref name="T4"/>.</exception>
        /// <param name="sum">Instance to unwrap.</param>
        public static explicit operator T4(SumType<T1, T2, T3, T4> sum) => sum.Value is T4 tVal ? tVal : throw new InvalidCastException();

        public static bool operator ==(SumType<T1, T2, T3, T4> left, SumType<T1, T2, T3, T4> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SumType<T1, T2, T3, T4> left, SumType<T1, T2, T3, T4> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Tries to get the value as the first specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetFirst([MaybeNullWhen(false)] out T1 value)
        {
            if (this.Value is T1 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the second specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetSecond([MaybeNullWhen(false)] out T2 value)
        {
            if (this.Value is T2 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the third specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetThird([MaybeNullWhen(false)] out T3 value)
        {
            if (this.Value is T3 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value as the fourth specified type.
        /// </summary>
        /// <param name="value">the value in the specified type/>.</param>
        /// <returns><see langword="true"/> if the type matches.</returns>
        public bool TryGetFourth([MaybeNullWhen(false)] out T4 value)
        {
            if (this.Value is T4 typeValue)
            {
                value = typeValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Runs a delegate corresponding to which type is contained inside this instance.
        /// </summary>
        /// <typeparam name="TResult">The type that all the delegates will return.</typeparam>
        /// <param name="firstMatch">Delegate to handle the case where this instance contains a <typeparamref name="T1"/>.</param>
        /// <param name="secondMatch">Delegate to handle the case where this instance contains a <typeparamref name="T2"/>.</param>
        /// <param name="thirdMatch">Delegate to handle the case where this instance contains a <typeparamref name="T3"/>.</param>
        /// <param name="fourthMatch">Delegate to handle the case where this instance contains a <typeparamref name="T4"/>.</param>
        /// <param name="defaultMatch">
        /// Delegate to handle the case where this instance is uninhabited. If this delegate isn't provided the default
        /// <typeparamref name="TResult"/> will be returned instead.
        /// </param>
        /// <returns>The <typeparamref name="TResult"/> instance created by the delegate corresponding to the current type stored in this instance.</returns>
        public TResult Match<TResult>(Func<T1, TResult> firstMatch, Func<T2, TResult> secondMatch, Func<T3, TResult> thirdMatch, Func<T4, TResult> fourthMatch, Func<TResult>? defaultMatch = null)
        {
            if (firstMatch == null)
            {
                throw new ArgumentNullException(nameof(firstMatch));
            }

            if (secondMatch == null)
            {
                throw new ArgumentNullException(nameof(secondMatch));
            }

            if (thirdMatch == null)
            {
                throw new ArgumentNullException(nameof(thirdMatch));
            }

            if (fourthMatch == null)
            {
                throw new ArgumentNullException(nameof(fourthMatch));
            }

            if (this.Value is T1 tOne)
            {
                return firstMatch(tOne);
            }

            if (this.Value is T2 tTwo)
            {
                return secondMatch(tTwo);
            }

            if (this.Value is T3 tThree)
            {
                return thirdMatch(tThree);
            }

            if (this.Value is T4 tFour)
            {
                return fourthMatch(tFour);
            }

            if (defaultMatch != null)
            {
                return defaultMatch();
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default(TResult);
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SumType<T1, T2, T3, T4> type && this.Equals(type);
        }

        /// <inheritdoc/>
        public bool Equals(SumType<T1, T2, T3, T4> other)
        {
            return EqualityComparer<object?>.Default.Equals(this.Value, other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return -1937169414 + EqualityComparer<object?>.Default.GetHashCode(this.Value);
        }
    }

    /// <summary>
    /// Utility methods for <see cref="ISumType"/> implementations.
    /// </summary>
    internal static class SumTypeUtils
    {
        /// <summary>
        /// Validates that <paramref name="type"/> is a valid type parameter for a SumType.
        /// </summary>
        /// <param name="type">The type to validate.</param>
        /// <exception cref="NotSupportedException">If <paramref name="type"/> is not supported as a type parameter for a
        /// SumType.</exception>
        public static void ValidateTypeParameter(Type type)
        {
            if (typeof(ISumType).IsAssignableFrom(type))
            {
                throw new NotSupportedException(LanguageServerProtocolResources.NestedSumType);
            }
        }
    }
}
