// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ReadOnlyCollection<EqualityUnit<T>> _equalityUnits;
        private readonly Func<T, T, bool> _compareWithEqualityOperator;
        private readonly Func<T, T, bool> _compareWithInequalityOperator;

        public EqualityUtil(
            IEnumerable<EqualityUnit<T>> equalityUnits,
            Func<T, T, bool> compEquality = null,
            Func<T, T, bool> compInequality = null)
        {
            _equalityUnits = equalityUnits.ToList().AsReadOnly();
            _compareWithEqualityOperator = compEquality;
            _compareWithInequalityOperator = compInequality;
        }

        public void RunAll()
        {
            if (_compareWithEqualityOperator != null)
            {
                EqualityOperator1();
                EqualityOperator2();
            }

            if (_compareWithInequalityOperator != null)
            {
                InequalityOperator1();
                InequalityOperator2();
            }

            ImplementsIEquatable();
            ObjectEquals1();
            ObjectEquals2();
            ObjectEquals3();
            GetHashCode1();
            EquatableEquals1();
            EquatableEquals2();
        }

        private void EqualityOperator1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(_compareWithEqualityOperator(unit.Value, value));
                    Assert.True(_compareWithEqualityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.False(_compareWithEqualityOperator(unit.Value, value));
                    Assert.False(_compareWithEqualityOperator(value, unit.Value));
                }
            }
        }

        private void EqualityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.False(_compareWithEqualityOperator(default(T), value));
                Assert.False(_compareWithEqualityOperator(value, default(T)));
            }
        }

        private void InequalityOperator1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.False(_compareWithInequalityOperator(unit.Value, value));
                    Assert.False(_compareWithInequalityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.True(_compareWithInequalityOperator(unit.Value, value));
                    Assert.True(_compareWithInequalityOperator(value, unit.Value));
                }
            }
        }

        private void InequalityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.True(_compareWithInequalityOperator(default(T), value));
                Assert.True(_compareWithInequalityOperator(value, default(T)));
            }
        }

        private void ImplementsIEquatable()
        {
            var type = typeof(T);
            var targetType = typeof(IEquatable<T>);
            Assert.True(type.GetTypeInfo().ImplementedInterfaces.Contains(targetType));
        }

        private void ObjectEquals1()
        {
            foreach (var unit in _equalityUnits)
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
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            var allValues = _equalityUnits.SelectMany(x => x.AllValues);
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
            var allValues = _equalityUnits.SelectMany(x => x.AllValues);
            foreach (var value in allValues)
            {
                Assert.NotEqual((object)42, value);
            }
        }

        private void GetHashCode1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.Equal(value.GetHashCode(), unit.Value.GetHashCode());
                }
            }
        }

        private void EquatableEquals1()
        {
            foreach (var unit in _equalityUnits)
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
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var cur in _equalityUnits.SelectMany(x => x.AllValues))
            {
                var value = (IEquatable<T>)cur;
                Assert.NotNull(value);
            }
        }
    }
}
