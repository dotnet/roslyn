using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Maps a constructor invocation to <see cref="PropertySetAbstractValueKind"/>s for the properties being tracked by PropertySetAnalysis.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class ConstructorMapper
#pragma warning restore CA1812
    {
        public delegate ImmutableArray<PropertySetAbstractValueKind> ValueContentAbstractValueCallback(
            IMethodSymbol constructorMethodSymbol,
            IReadOnlyList<ValueContentAbstractValue> argumentValueContentAbstractValues);

        /// <summary>
        /// Initializes a <see cref="ConstructorMapper"/> that maps a constructor invocation's arguments' <see cref="ValueContentAbstractValue"/> to <see cref="PropertySetAbstractValueKind"/>s for the properties being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="mapFromValueContentAbstractValueCallback">Callback that implements the mapping.</param>
        public ConstructorMapper(ValueContentAbstractValueCallback mapFromValueContentAbstractValue)
        {
            this.MapFromConstructorInvocation = mapFromValueContentAbstractValue ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValue));
        }

        /// <summary>
        /// Initializes a <see cref="ConstructorMapper"/> using constant <see cref="PropertySetAbstractValueKind"/>s whenever the type being tracked by PropertySetAnalysis is instantiated.
        /// </summary>
        /// <param name="propertyAbstractValues">Constant <see cref="PropertySetAbstractValueKind"/>s, in the same order that the corresponding <see cref="PropertyMapperCollection"/> was initialized with.</param>
        public ConstructorMapper(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues)
        {
            this.PropertyAbstractValues = propertyAbstractValues;
        }

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private ConstructorMapper()
        {
        }


        public ValueContentAbstractValueCallback MapFromConstructorInvocation { get; }

        public ImmutableArray<PropertySetAbstractValueKind> PropertyAbstractValues { get; }



        internal void Validate(int propertyCount)
        {
            if (this.PropertyAbstractValues != null)
            {
                if (this.PropertyAbstractValues.Length != propertyCount)
                {
                    throw new ArgumentException($"ConstructorMapper PropertyAbstractValues has invalid length (expected {propertyCount}, actual length {this.PropertyAbstractValues.Length})");
                }
            }
        }

        internal ImmutableArray<PropertySetAbstractValueKind> Compute(IObjectCreationOperation objectCreationOperation)
        {
            if (this.PropertyAbstractValues != null)
            {
                return this.PropertyAbstractValues;
            }
            else
            {
                Debug.Fail("TODO handle ValueContentAbstractValues for arguments");
                return this.MapFromConstructorInvocation(objectCreationOperation.Constructor, null);
            }
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(
                this.PropertyAbstractValues,
                MapFromConstructorInvocation.GetHashCodeOrDefault());
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ConstructorMapper);
        }

        public bool Equals(ConstructorMapper other)
        {
            return other != null
                && this.MapFromConstructorInvocation == other.MapFromConstructorInvocation
                && this.PropertyAbstractValues == other.PropertyAbstractValues;
        }
    }
}
