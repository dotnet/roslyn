// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Base class which does a lot of the boiler plate work for testing that the equality pattern
    /// is properly implemented in objects
    /// </summary>
    public sealed class EqualityUtil<T>
    {
        private readonly ReadOnlyCollection<EqualityUnit<T>> equalityUnits;
        private readonly Func<T, T, bool> compareWithEqualityOperator;
        private readonly Func<T, T, bool> compareWithInequalityOperator;

        public EqualityUtil(
            IEnumerable<EqualityUnit<T>> equalityUnits,
            Func<T, T, bool> compEquality,
            Func<T, T, bool> compInequality)
        {
            this.equalityUnits = equalityUnits.ToList().AsReadOnly();
            this.compareWithEqualityOperator = compEquality;
            this.compareWithInequalityOperator = compInequality;
        }

        public void RunAll()
        {
            var methods = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                method.Invoke(this, null);
            }
        }

        private void EqualityOperator1()
        {
            foreach (var unit in equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(compareWithEqualityOperator(unit.Value, value));
                    Assert.True(compareWithEqualityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.False(compareWithEqualityOperator(unit.Value, value));
                    Assert.False(compareWithEqualityOperator(value, unit.Value));
                }
            }
        }

        private void EqualityOperator2()
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            foreach (var value in equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.False(compareWithEqualityOperator(default(T), value));
                Assert.False(compareWithEqualityOperator(value, default(T)));
            }
        }

        private void InEqualityOperator1()
        {
            foreach (var unit in equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.False(compareWithInequalityOperator(unit.Value, value));
                    Assert.False(compareWithInequalityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.True(compareWithInequalityOperator(unit.Value, value));
                    Assert.True(compareWithInequalityOperator(value, unit.Value));
                }
            }
        }

        private void InEqualityOperator2()
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            foreach (var value in equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.True(compareWithInequalityOperator(default(T), value));
                Assert.True(compareWithInequalityOperator(value, default(T)));
            }
        }

        private void ImplementsIEquatable()
        {
            var type = typeof(T);
            var targetType = typeof(IEquatable<T>);
            Assert.True(type.GetInterfaces().Contains(targetType));
        }

        private void ObjectEquals1()
        {
            foreach (var unit in equalityUnits)
            {
                var unitValue = unit.Value;
                foreach (var value in unit.EqualValues)
                {
                    Assert.Equal(value, unitValue);
                    Assert.Equal(unitValue, value);
                }
            }
        }

        /// <summary>
        /// Comparison with Null should be false for reference types
        /// </summary>
        private void ObjectEquals2()
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            var allValues = equalityUnits.SelectMany(x => x.AllValues);
            foreach (var value in allValues)
            {
                Assert.NotNull(value);
            }
        }

        /// <summary>
        /// Passing a value of a different type should just return false
        /// </summary>
        private void ObjectEquals3()
        {
            var allValues = equalityUnits.SelectMany(x => x.AllValues);
            foreach (var value in allValues)
            {
                Assert.NotEqual((object)42, value);
            }
        }

        private void GetHashCode1()
        {
            foreach (var unit in equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.Equal(value.GetHashCode(), unit.Value.GetHashCode());
                }
            }
        }

        private void EquatableEquals1()
        {
            foreach (var unit in equalityUnits)
            {
                var equatableUnit = (IEquatable<T>)unit.Value;
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(equatableUnit.Equals(value));
                    var equatableValue = (IEquatable<T>)value;
                    Assert.True(equatableValue.Equals(unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.False(equatableUnit.Equals(value));
                    var equatableValue = (IEquatable<T>)value;
                    Assert.False(equatableValue.Equals(unit.Value));
                }
            }
        }

        /// <summary>
        /// If T is a reference type, null should return false in all cases
        /// </summary>
        private void EquatableEquals2()
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            foreach (var cur in equalityUnits.SelectMany(x => x.AllValues))
            {
                var value = (IEquatable<T>)cur;
                Assert.NotNull(value);
            }
        }
    }
}