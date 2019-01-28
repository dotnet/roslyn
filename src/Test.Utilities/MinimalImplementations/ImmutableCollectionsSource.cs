// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class ImmutableCollectionsSource
    {
        public const string CSharp = @"
using System.Collections.Generic;
using System.Collections.Immutable;
using static System.Collections.Immutable.ImmutableExtensions;
namespace System.Collections.Immutable
{
    public sealed partial class ImmutableArray<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class ImmutableList<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class ImmutableHashSet<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class ImmutableSortedSet<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class ImmutableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class ImmutableSortedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public static class ImmutableExtensions
    {
        public static ImmutableArray<T> ToImmutableArray<T>(this IEnumerable<T> source)
        {
            return null;
        }
        public static ImmutableArray<T> ToImmutableArray<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return null;
        }
        public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> source)
        {
            return null;
        }
        public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return null;
        }
        public static ImmutableHashSet<T> ToImmutableHashSet<T>(this IEnumerable<T> source)
        {
            return null;
        }
        public static ImmutableHashSet<T> ToImmutableHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return null;
        }
        public static ImmutableSortedSet<T> ToImmutableSortedSet<T>(this IEnumerable<T> source)
        {
            return null;
        }
        public static ImmutableSortedSet<T> ToImmutableSortedSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return null;
        }
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return null;
        }
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> keyComparer)
        {
            return null;
        }
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return null;
        }
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> keyComparer)
        {
            return null;
        }
    }
}
";

        public const string Basic = @"
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.Immutable.ImmutableExtensions
Imports System.Runtime.CompilerServices
Namespace System.Collections.Immutable
    Partial Public NotInheritable Class ImmutableArray(Of T)
        Implements IEnumerable(Of T)
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Partial Public NotInheritable Class ImmutableList(Of T)
        Implements IEnumerable(Of T)
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Partial Public NotInheritable Class ImmutableHashSet(Of T)
        Implements IEnumerable(Of T)
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Partial Public NotInheritable Class ImmutableSortedSet(Of T)
        Implements IEnumerable(Of T)
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Partial Public NotInheritable Class ImmutableDictionary(Of TKey, TValue)
        Implements IEnumerable(Of KeyValuePair(Of TKey, TValue))
        Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of TKey, TValue)) Implements IEnumerable(Of KeyValuePair(Of TKey, TValue)).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Partial Public NotInheritable Class ImmutableSortedDictionary(Of TKey, TValue)
        Implements IEnumerable(Of KeyValuePair(Of TKey, TValue))
        Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of TKey, TValue)) Implements IEnumerable(Of KeyValuePair(Of TKey, TValue)).GetEnumerator
            Throw New NotImplementedException()
        End Function
        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
    Module ImmutableExtensions
        <Extension()>
        Function ToImmutableArray(Of T)(ByVal source As IEnumerable(Of T)) As ImmutableArray(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableArray(Of T)(ByVal source As IEnumerable(Of T), ByVal comparer As IEqualityComparer(Of T)) As ImmutableArray(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableList(Of T)(ByVal source As IEnumerable(Of T)) As ImmutableList(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableList(Of T)(ByVal source As IEnumerable(Of T), ByVal comparer As IEqualityComparer(Of T)) As ImmutableList(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableHashSet(Of T)(ByVal source As IEnumerable(Of T)) As ImmutableHashSet(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableHashSet(Of T)(ByVal source As IEnumerable(Of T), ByVal comparer As IEqualityComparer(Of T)) As ImmutableHashSet(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableSortedSet(Of T)(ByVal source As IEnumerable(Of T)) As ImmutableSortedSet(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableSortedSet(Of T)(ByVal source As IEnumerable(Of T), ByVal comparer As IEqualityComparer(Of T)) As ImmutableSortedSet(Of T)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableDictionary(Of TKey, TValue)(ByVal source As IEnumerable(Of KeyValuePair(Of TKey, TValue))) As ImmutableDictionary(Of TKey, TValue)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableDictionary(Of TKey, TValue)(ByVal source As IEnumerable(Of KeyValuePair(Of TKey, TValue)), ByVal keyComparer As IEqualityComparer(Of TKey)) As ImmutableDictionary(Of TKey, TValue)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableSortedDictionary(Of TKey, TValue)(ByVal source As IEnumerable(Of KeyValuePair(Of TKey, TValue))) As ImmutableSortedDictionary(Of TKey, TValue)
            Return Nothing
        End Function
        <Extension()>
        Function ToImmutableSortedDictionary(Of TKey, TValue)(ByVal source As IEnumerable(Of KeyValuePair(Of TKey, TValue)), ByVal keyComparer As IEqualityComparer(Of TKey)) As ImmutableSortedDictionary(Of TKey, TValue)
            Return Nothing
        End Function
    End Module
End Namespace
";
    }
}
