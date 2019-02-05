using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    //using ConstructorInvocationCallback = Func<IMethodSymbol, IReadOnlyList<ValueContentAbstractValue>, PropertySetAbstractValueKind[]>;

    /// <summary>
    /// Maps a constructor invocation to <see cref="PropertySetAbstractValueKind" />s.
    /// </summary>
    internal sealed class ConstructorMapper
    {
        public delegate ImmutableArray<PropertySetAbstractValueKind> ConstructorInvocationCallback(
            IMethodSymbol constructorMethodSymbol,
            IReadOnlyList<ValueContentAbstractValue> argumentValueContentAbstractValues);

        /// <summary>
        /// Constructs using a callback to examine constructor invocations to determine <see cref="PropertySetAbstractValueKind"/>s.
        /// </summary>
        /// <param name="callback"></param>
        public ConstructorMapper(ConstructorInvocationCallback callback)
        {
            this.OnConstructorInvocation = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <summary>
        /// Constructs using constant <see cref="PropertySetAbstractValueKind"/>s whenever the type is constructed.
        /// </summary>
        /// <param name="propertyAbstractValues"></param>
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

        public ConstructorInvocationCallback OnConstructorInvocation { get; }

        public ImmutableArray<PropertySetAbstractValueKind> PropertyAbstractValues { get; }
    }
}
