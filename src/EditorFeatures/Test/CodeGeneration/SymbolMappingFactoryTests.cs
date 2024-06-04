// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Xunit;

#pragma warning disable VSTHRD104 // Offer async methods (this file intentionally tests cases that would not appear in the same form in other source)
#pragma warning disable CA1822 // Mark members as static (this file intentionally tests cases that would not appear in the same form in other source)
#pragma warning disable CA2012 // Use ValueTasks correctly (this file intentionally tests cases that would not appear in the same form in other source)

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public class SymbolMappingFactoryTests
    {
        public class MultipleConstructors
        {
            [Fact]
            public void TestFirstConstructor()
            {
                var instance = Factory.Instance.Create();
                Assert.Equal(0, instance.ConstructorIndex);
                Assert.Equal(0, instance.Value);
            }

            [Fact]
            public void TestSecondConstructor()
            {
                var instance = Factory.Instance.Create(1);
                Assert.Equal(1, instance.ConstructorIndex);
                Assert.Equal(1, instance.Value);

                instance = Factory.Instance.Create(2);
                Assert.Equal(1, instance.ConstructorIndex);
                Assert.Equal(2, instance.Value);
            }

            public interface IInterface
            {
                int ConstructorIndex { get; }
                int Value { get; }
            }

            internal interface ICodeGenerationInterface
            {
                int ConstructorIndex { get; }
                int Value { get; }
            }

            internal abstract class CodeGenerationType : ICodeGenerationInterface
            {
                protected CodeGenerationType()
                {
                    ConstructorIndex = 0;
                    Value = 0;
                }

                protected CodeGenerationType(int value)
                {
                    ConstructorIndex = 1;
                    Value = value;
                }

                public int ConstructorIndex { get; }
                public int Value { get; }
            }

            private sealed class Factory : SymbolMappingFactory
            {
                private static readonly FrozenDictionary<Type, Type> s_interfaceMapping = FrozenDictionary.ToFrozenDictionary(
                    [
                        (typeof(ICodeGenerationInterface), typeof(IInterface)),
                    ],
                    pair => pair.Item1,
                    pair => pair.Item2);

                public static readonly Factory Instance = new();

                private Factory()
                    : base(s_interfaceMapping)
                {
                }

                public IInterface Create()
                {
                    var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationType));
                    Assert.Same(implementationType, GetOrCreateImplementationType(typeof(CodeGenerationType)));

                    var constructor = (Func<CodeGenerationType>)GetOrCreateConstructor(implementationType, []);
                    Assert.Same(constructor, GetOrCreateConstructor(implementationType, []));

                    return (IInterface)constructor();
                }

                public IInterface Create(int value)
                {
                    var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationType));
                    Assert.Same(implementationType, GetOrCreateImplementationType(typeof(CodeGenerationType)));

                    var constructor = (Func<int, CodeGenerationType>)GetOrCreateConstructor(implementationType, [typeof(int)]);
                    Assert.Same(constructor, GetOrCreateConstructor(implementationType, [typeof(int)]));

                    return (IInterface)constructor(value);
                }
            }
        }

        public class DefaultResults
        {
            [Fact]
            public void TestMethods()
            {
                var instance = Factory.Instance.Create();
                Assert.Throws<NotSupportedException>(instance.MethodVoid);
                Assert.Equal(0, instance.MethodInt32());
                Assert.Null(instance.MethodObject());

                // ImmutableArray<T> is supported to return empty, but most collections just return null
                Assert.Equal(ImmutableArray<int>.Empty, instance.MethodImmutableArray());
                Assert.Null(instance.MethodEnumerable());

                // Asynchronous methods are not explicitly supported
                Assert.Null(instance.MethodTaskAsync());
                Assert.Null(instance.MethodTaskTAsync());
                Assert.True(instance.MethodValueTaskAsync().IsCompletedSuccessfully);
                Assert.True(instance.MethodValueTaskTAsync().IsCompletedSuccessfully);
                Assert.True(instance.MethodValueTaskTAsync().Result.IsDefault);
            }

            [Fact]
            public void TestPropertyGetters()
            {
                var instance = Factory.Instance.Create();
                Assert.Equal(0, instance.ReadOnlyPropertyInt32);
                Assert.Null(instance.ReadOnlyPropertyObject);

                Assert.Equal(0, instance.ReadWritePropertyInt32);
                Assert.Null(instance.ReadWritePropertyObject);

                // ImmutableArray<T> is supported to return empty, but most collections just return null
                Assert.Equal(ImmutableArray<int>.Empty, instance.ReadOnlyPropertyImmutableArray);
                Assert.Null(instance.ReadOnlyPropertyEnumerable);

                Assert.Equal(ImmutableArray<int>.Empty, instance.ReadWritePropertyImmutableArray);
                Assert.Null(instance.ReadWritePropertyEnumerable);

                // Asynchronous methods are not explicitly supported. ValueTask<> returns a default instance, which is
                // observably in a successfully-completed state with a default result.
                Assert.Null(instance.ReadOnlyPropertyTask);
                Assert.Null(instance.ReadOnlyPropertyTaskT);
                Assert.True(instance.ReadOnlyPropertyValueTask.IsCompletedSuccessfully);
                Assert.True(instance.ReadOnlyPropertyValueTaskT.IsCompletedSuccessfully);
                Assert.True(instance.ReadOnlyPropertyValueTaskT.Result.IsDefault);

                Assert.Null(instance.ReadWritePropertyTask);
                Assert.Null(instance.ReadWritePropertyTaskT);
                Assert.True(instance.ReadWritePropertyValueTask.IsCompletedSuccessfully);
                Assert.True(instance.ReadWritePropertyValueTaskT.IsCompletedSuccessfully);
                Assert.True(instance.ReadWritePropertyValueTaskT.Result.IsDefault);
            }

            [Fact]
            public void TestPropertySetters()
            {
                var instance = Factory.Instance.Create();
                Assert.Throws<NotSupportedException>(() => instance.WriteOnlyPropertyInt32 = default);
                Assert.Throws<NotSupportedException>(() => instance.WriteOnlyPropertyObject = null);

                Assert.Throws<NotSupportedException>(() => instance.ReadWritePropertyInt32 = default);
                Assert.Throws<NotSupportedException>(() => instance.ReadWritePropertyObject = null);

                // ImmutableArray<T> is supported to return empty, but most collections just return null
                Assert.Throws<NotSupportedException>(() => instance.WriteOnlyPropertyImmutableArray = default);
                Assert.Throws<NotSupportedException>(() => instance.WriteOnlyPropertyEnumerable = null!);

                Assert.Throws<NotSupportedException>(() => instance.ReadWritePropertyImmutableArray = default);
                Assert.Throws<NotSupportedException>(() => instance.ReadWritePropertyEnumerable = null!);

                // Asynchronous methods are not explicitly supported. ValueTask<> returns a default instance, which is
                // observably in a successfully-completed state with a default result.
                Assert.Throws<NotSupportedException>(void () => instance.WriteOnlyPropertyTask = null!);
                Assert.Throws<NotSupportedException>(void () => instance.WriteOnlyPropertyTaskT = null!);
                Assert.Throws<NotSupportedException>(void () => instance.WriteOnlyPropertyValueTask = default);
                Assert.Throws<NotSupportedException>(void () => instance.WriteOnlyPropertyValueTaskT = default);

                Assert.Throws<NotSupportedException>(void () => instance.ReadWritePropertyTask = null!);
                Assert.Throws<NotSupportedException>(void () => instance.ReadWritePropertyTaskT = null!);
                Assert.Throws<NotSupportedException>(void () => instance.ReadWritePropertyValueTask = default);
                Assert.Throws<NotSupportedException>(void () => instance.ReadWritePropertyValueTaskT = default);
            }

            public interface IInterface
            {
                void MethodVoid();
                int MethodInt32();
                object? MethodObject();
                ImmutableArray<int> MethodImmutableArray();
                IEnumerable<int> MethodEnumerable();
                Task MethodTaskAsync();
                Task<int> MethodTaskTAsync();
                ValueTask MethodValueTaskAsync();
                ValueTask<ImmutableArray<int>> MethodValueTaskTAsync();

                int ReadOnlyPropertyInt32 { get; }
                object? ReadOnlyPropertyObject { get; }
                ImmutableArray<int> ReadOnlyPropertyImmutableArray { get; }
                IEnumerable<int> ReadOnlyPropertyEnumerable { get; }
                Task ReadOnlyPropertyTask { get; }
                Task<int> ReadOnlyPropertyTaskT { get; }
                ValueTask ReadOnlyPropertyValueTask { get; }
                ValueTask<ImmutableArray<int>> ReadOnlyPropertyValueTaskT { get; }

                int WriteOnlyPropertyInt32 { set; }
                object? WriteOnlyPropertyObject { set; }
                ImmutableArray<int> WriteOnlyPropertyImmutableArray { set; }
                IEnumerable<int> WriteOnlyPropertyEnumerable { set; }
                Task WriteOnlyPropertyTask { set; }
                Task<int> WriteOnlyPropertyTaskT { set; }
                ValueTask WriteOnlyPropertyValueTask { set; }
                ValueTask<ImmutableArray<int>> WriteOnlyPropertyValueTaskT { set; }

                int ReadWritePropertyInt32 { get; set; }
                object? ReadWritePropertyObject { get; set; }
                ImmutableArray<int> ReadWritePropertyImmutableArray { get; set; }
                IEnumerable<int> ReadWritePropertyEnumerable { get; set; }
                Task ReadWritePropertyTask { get; set; }
                Task<int> ReadWritePropertyTaskT { get; set; }
                ValueTask ReadWritePropertyValueTask { get; set; }
                ValueTask<ImmutableArray<int>> ReadWritePropertyValueTaskT { get; set; }
            }

            internal interface ICodeGenerationInterface
            {
                // Intentionally does not shadow any members from IInterface
            }

            internal abstract class CodeGenerationType : ICodeGenerationInterface
            {
            }

            private sealed class Factory : SymbolMappingFactory
            {
                private static readonly FrozenDictionary<Type, Type> s_interfaceMapping = FrozenDictionary.ToFrozenDictionary(
                    [
                        (typeof(ICodeGenerationInterface), typeof(IInterface)),
                    ],
                    pair => pair.Item1,
                    pair => pair.Item2);

                public static readonly Factory Instance = new();

                private Factory()
                    : base(s_interfaceMapping)
                {
                }

                public IInterface Create()
                {
                    var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationType));
                    Assert.Same(implementationType, GetOrCreateImplementationType(typeof(CodeGenerationType)));

                    var constructor = (Func<CodeGenerationType>)GetOrCreateConstructor(implementationType, []);
                    Assert.Same(constructor, GetOrCreateConstructor(implementationType, []));

                    return (IInterface)constructor();
                }
            }
        }

        public class AddSymbolInterface
        {
            [Fact]
            public void TestNewMember()
            {
                var instance = Factory.Instance.Create();
                Assert.Same(CodeGenerationType.KnownFirstValue, instance.ExplicitShadowValue);
                Assert.Same(CodeGenerationType.KnownThirdValue, instance.ImplicitShadowValue);
                Assert.Same(CodeGenerationType.KnownSecondValue, instance.SecondValue);

                // The IEquatable<IInterface> implementation exists on both ICodeGenerationInterface and IInterface.
                // Verify that the implementation is preserved during generation of the new type (a default
                // implementation of a bool-returning method would return false for this call).
                Assert.True(instance.Equals(instance));

                // The new base interface has no mapping, so its members return default values even if another member
                // with the same signature existed in the derived interface.
                Assert.Null(((INewBaseInterface)instance).ExplicitShadowValue);
                Assert.Null(((INewBaseInterface)instance).ImplicitShadowValue);
                Assert.Null(((INewBaseInterface)instance).AccidentalShadowValue);
            }

            public interface INewBaseInterface
            {
                object? ExplicitShadowValue { get; }
                object? ImplicitShadowValue { get; }

                // This property has the same name as a property in CodeGenerationType, but it isn't part of any mapping
                object? AccidentalShadowValue { get; }
            }

            public interface IInterface : IEquatable<IInterface>, INewBaseInterface
            {
                new object? ExplicitShadowValue { get; }
                new object? ImplicitShadowValue { get; }
                object? SecondValue { get; }
            }

            internal interface ICodeGenerationInterface : IEquatable<IInterface>
            {
                object? ExplicitShadowValue { get; }
                object? ImplicitShadowValue { get; }
                object? SecondValue { get; }
            }

            internal abstract class CodeGenerationType : ICodeGenerationInterface
            {
                public static readonly object KnownFirstValue = new();
                public static readonly object KnownSecondValue = new();
                public static readonly object KnownThirdValue = new();
                public static readonly object KnownFourthValue = new();

                object? ICodeGenerationInterface.ExplicitShadowValue => KnownFirstValue;

                public object? ImplicitShadowValue => KnownThirdValue;

                object? ICodeGenerationInterface.SecondValue => KnownSecondValue;

                public object? AccidentalShadowValue => KnownFourthValue;

                public bool Equals(IInterface other)
                    => this == other;
            }

            private sealed class Factory : SymbolMappingFactory
            {
                private static readonly FrozenDictionary<Type, Type> s_interfaceMapping = FrozenDictionary.ToFrozenDictionary(
                    [
                        (typeof(ICodeGenerationInterface), typeof(IInterface)),
                    ],
                    pair => pair.Item1,
                    pair => pair.Item2);

                public static readonly Factory Instance = new();

                private Factory()
                    : base(s_interfaceMapping)
                {
                }

                public IInterface Create()
                {
                    var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationType));
                    Assert.Same(implementationType, GetOrCreateImplementationType(typeof(CodeGenerationType)));

                    var constructor = (Func<CodeGenerationType>)GetOrCreateConstructor(implementationType, []);
                    Assert.Same(constructor, GetOrCreateConstructor(implementationType, []));

                    return (IInterface)constructor();
                }
            }
        }
    }
}
