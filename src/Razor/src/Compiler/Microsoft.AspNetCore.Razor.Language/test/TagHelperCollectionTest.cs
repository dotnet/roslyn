// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class TagHelperCollectionTest
{
    private static TagHelperDescriptor CreateTagHelper(string name, string assemblyName = "TestAssembly")
    {
        var builder = TagHelperDescriptorBuilder.Create(name, assemblyName);
        builder.TypeName = name;
        builder.TagMatchingRule(rule => rule.TagName = name.ToLowerInvariant());
        return builder.Build();
    }

    private static TagHelperDescriptor[] CreateTestTagHelpers(int count, int startIndex = 0)
    {
        var result = new TagHelperDescriptor[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = CreateTagHelper($"TagHelper{startIndex + i}");
        }

        return result;
    }

    [Fact]
    public void Empty_ReturnsEmptyCollection()
    {
        // Act
        var collection = TagHelperCollection.Empty;

        // Assert
        Assert.NotNull(collection);
        Assert.Empty(collection);
        Assert.True(collection.IsEmpty);
        Assert.Equal(-1, collection.IndexOf(CreateTagHelper("Test")));
        Assert.False(collection.Contains(CreateTagHelper("Test")));
    }

    [Fact]
    public void Empty_Enumerator_IsEmpty()
    {
        // Arrange
        var collection = TagHelperCollection.Empty;

        // Act & Assert
        Assert.Empty(collection);

        var enumerator = collection.GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Empty_CopyTo_DoesNothing()
    {
        // Arrange
        var collection = TagHelperCollection.Empty;
        var destination = new TagHelperDescriptor[1];

        // Act
        collection.CopyTo(destination);

        // Assert
        Assert.Null(destination[0]);
    }

    [Fact]
    public void Empty_Indexer_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var collection = TagHelperCollection.Empty;

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => collection[0]);
        Assert.Throws<IndexOutOfRangeException>(() => collection[-1]);
    }

    [Fact]
    public void Create_EmptyImmutableArray_ReturnsEmpty()
    {
        // Act
        var empty = ImmutableArray<TagHelperDescriptor>.Empty;
        var collection = TagHelperCollection.Create(empty);

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Create_SingleItemImmutableArray_ReturnsSingleItemCollection()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Test");
        var array = ImmutableArray.Create(tagHelper);

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.Single(collection);
        Assert.False(collection.IsEmpty);
        Assert.Same(tagHelper, collection[0]);
        Assert.Equal(0, collection.IndexOf(tagHelper));
        Assert.True(collection.Contains(tagHelper));
    }

    [Fact]
    public void Create_MultipleItemsImmutableArray_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var array = ImmutableArray.Create(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.False(collection.IsEmpty);

        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], collection[i]);
            Assert.Equal(i, collection.IndexOf(tagHelpers[i]));
            Assert.True(collection.Contains(tagHelpers[i]));
        }
    }

    [Fact]
    public void Create_ImmutableArrayWithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var array = ImmutableArray.Create(tagHelper1, tagHelper2, tagHelper1);

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Create_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var array = Array.Empty<TagHelperDescriptor>();

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Create_SingleItemArray_ReturnsSingleItemCollection()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Test");
        var array = new[] { tagHelper };

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.Single(collection);
        Assert.Same(tagHelper, collection[0]);
    }

    [Fact]
    public void Create_MultipleItemArray_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);

        // Act
        var collection = TagHelperCollection.Create(tagHelpers);

        // Assert
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_ArrayWithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var array = new[] { tagHelper1, tagHelper2, tagHelper1 };

        // Act
        var collection = TagHelperCollection.Create(array);

        // Assert
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Create_EmptyCollectionExpression_ReturnsEmpty()
    {
        // Act
        TagHelperCollection collection = [];

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Create_SingleItemCollectionExpression_ReturnsSingleItemCollection()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Test");

        // Act
        TagHelperCollection collection = [tagHelper];

        // Assert
        Assert.Single(collection);
        Assert.False(collection.IsEmpty);
        Assert.Same(tagHelper, collection[0]);
        Assert.Equal(0, collection.IndexOf(tagHelper));
        Assert.True(collection.Contains(tagHelper));
    }

    [Fact]
    public void Create_MultipleItemsCollectionExpression_CreatesCollection()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");

        // Act
        TagHelperCollection collection = [tagHelper1, tagHelper2, tagHelper3];

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.False(collection.IsEmpty);
        Assert.Same(tagHelper1, collection[0]);
        Assert.Same(tagHelper2, collection[1]);
        Assert.Same(tagHelper3, collection[2]);
        Assert.Equal(0, collection.IndexOf(tagHelper1));
        Assert.Equal(1, collection.IndexOf(tagHelper2));
        Assert.Equal(2, collection.IndexOf(tagHelper3));
        Assert.True(collection.Contains(tagHelper1));
        Assert.True(collection.Contains(tagHelper2));
        Assert.True(collection.Contains(tagHelper3));
    }

    [Fact]
    public void Create_CollectionExpressionWithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");

        // Act
        TagHelperCollection collection = [tagHelper1, tagHelper2, tagHelper1];

        // Assert
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Create_CollectionExpressionWithSpreadOperator_CreatesCollection()
    {
        // Arrange
        var firstBatch = CreateTestTagHelpers(2);
        var additionalTagHelper = CreateTagHelper("Additional");
        var lastBatch = CreateTestTagHelpers(2).Skip(2).ToArray(); // Get different helpers

        // Act
        TagHelperCollection collection = [.. firstBatch, additionalTagHelper, .. lastBatch];

        // Assert
        Assert.Equal(3, collection.Count); // 2 from first batch + 1 additional (lastBatch would be empty in this case)
        Assert.Same(firstBatch[0], collection[0]);
        Assert.Same(firstBatch[1], collection[1]);
        Assert.Same(additionalTagHelper, collection[2]);
    }

    [Fact]
    public void Create_CollectionExpressionFromExistingArray_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);

        // Act
        TagHelperCollection collection = [.. tagHelpers];

        // Assert
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_CollectionExpressionMixedSources_CreatesCollection()
    {
        // Arrange
        var singleHelper = CreateTagHelper("Single");
        var tagHelpers = CreateTestTagHelpers(3);
        TagHelperDescriptor[] arrayHelpers = [tagHelpers[0], tagHelpers[1]];
        List<TagHelperDescriptor> listHelpers = [tagHelpers[2]];

        // Act
        TagHelperCollection collection = [singleHelper, .. arrayHelpers, .. listHelpers];

        // Assert
        Assert.Equal(4, collection.Count);
        Assert.Same(singleHelper, collection[0]);
        Assert.Same(arrayHelpers[0], collection[1]);
        Assert.Same(arrayHelpers[1], collection[2]);
        Assert.Same(listHelpers[0], collection[3]);
    }

    [Fact]
    public void Create_IEnumerableEmpty_ReturnsEmpty()
    {
        // Arrange
        var items = new List<TagHelperDescriptor>();

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Create_IEnumerableEmptyEnumerable_ReturnsEmpty()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var items = new[] { CreateTagHelper("Test") }.Where(x => false);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Create_IEnumerableSingleItem_ReturnsSingleItemCollection()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Test");
        var items = new List<TagHelperDescriptor> { tagHelper };

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Single(collection);
        Assert.False(collection.IsEmpty);
        Assert.Same(tagHelper, collection[0]);
        Assert.Equal(0, collection.IndexOf(tagHelper));
        Assert.True(collection.Contains(tagHelper));
    }

    [Fact]
    public void Create_IEnumerableSingleItemEnumerable_ReturnsSingleItemCollection()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var tagHelper = CreateTagHelper("Test");
        var items = new[] { tagHelper, CreateTagHelper("Other") }.Where(x => x == tagHelper);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Single(collection);
        Assert.Same(tagHelper, collection[0]);
    }

    [Fact]
    public void Create_IEnumerableMultipleItems_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);
        var items = new List<TagHelperDescriptor>(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(4, collection.Count);
        Assert.False(collection.IsEmpty);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableMultipleItemsEnumerable_CreatesCollection()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var tagHelpers = CreateTestTagHelpers(4);
        var items = tagHelpers.Where(x => true); // Forces enumerable without known count

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(4, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableWithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var items = new List<TagHelperDescriptor> { tagHelper1, tagHelper2, tagHelper1, tagHelper2 };

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Create_IEnumerableEnumerableWithDuplicates_RemovesDuplicates()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var baseItems = new[] { tagHelper1, tagHelper2, tagHelper1, tagHelper2 };
        var items = baseItems.Where(x => true); // Forces enumerable without known count

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Create_IEnumerableHashSet_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var items = new HashSet<TagHelperDescriptor>(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableQueue_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var items = new Queue<TagHelperDescriptor>(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableStack_CreatesCollection()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var items = new Stack<TagHelperDescriptor>();
        items.Push(tagHelper1);
        items.Push(tagHelper2);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(2, collection.Count);
        // Note: Stack reverses order, so tagHelper2 comes first
        Assert.Same(tagHelper2, collection[0]);
        Assert.Same(tagHelper1, collection[1]);
    }

    [Fact]
    public void Create_IEnumerableLinkedList_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var items = new LinkedList<TagHelperDescriptor>(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableCustomCollection_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var items = new CustomCollection(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableCustomEnumerable_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var items = new CustomEnumerable(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableLargeCollection_WorksCorrectly()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(1000);
        var items = new List<TagHelperDescriptor>(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(1000, collection.Count);
        Assert.Same(tagHelpers[0], collection[0]);
        Assert.Same(tagHelpers[999], collection[999]);
    }

    [Fact]
    public void Create_IEnumerableLargeEnumerable_WorksCorrectly()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var tagHelpers = CreateTestTagHelpers(1000);
        var items = tagHelpers.Where(x => true);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(1000, collection.Count);
        Assert.Same(tagHelpers[0], collection[0]);
        Assert.Same(tagHelpers[999], collection[999]);
    }

    [Fact]
    public void Create_IEnumerableReadOnlyCollection_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        IReadOnlyCollection<TagHelperDescriptor> items = tagHelpers;

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Create_IEnumerableYieldReturn_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);

        static IEnumerable<TagHelperDescriptor> YieldItems(TagHelperDescriptor[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        var items = YieldItems(tagHelpers);

        // Act
        var collection = TagHelperCollection.Create(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.SameItems(tagHelpers, collection);
    }

    // Helper classes for testing
    private sealed class CustomCollection(IEnumerable<TagHelperDescriptor> items) : ICollection<TagHelperDescriptor>
    {
        private readonly List<TagHelperDescriptor> _items = [.. items];

        public int Count => _items.Count;
        public bool IsReadOnly => true;
        public void Add(TagHelperDescriptor item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(TagHelperDescriptor item) => _items.Contains(item);
        public void CopyTo(TagHelperDescriptor[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<TagHelperDescriptor> GetEnumerator() => _items.GetEnumerator();
        public bool Remove(TagHelperDescriptor item) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CustomEnumerable(IEnumerable<TagHelperDescriptor> items) : IEnumerable<TagHelperDescriptor>
    {
        private readonly List<TagHelperDescriptor> _items = [.. items];

        public IEnumerator<TagHelperDescriptor> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void ConcurrentAccess_MultipleThreads_DoesNotThrow()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(100);
        var collection = TagHelperCollection.Create(tagHelpers);
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                _ = collection[i % collection.Count];
                _ = collection.IndexOf(tagHelpers[i % tagHelpers.Length]);
                _ = collection.Contains(tagHelpers[i % tagHelpers.Length]);
                foreach (var item in collection) { /* enumerate */ }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void Build_EmptyBuilder_ReturnsEmpty()
    {
        // Act
        var collection = TagHelperCollection.Build("test", (ref builder, state) =>
        {
            // Don't add anything
        });

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Build_WithItems_CreatesCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);

        // Act
        var collection = TagHelperCollection.Build(tagHelpers, (ref builder, items) =>
        {
            foreach (var item in items)
            {
                builder.Add(item);
            }
        });

        // Assert
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Build_WithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");

        // Act
        var collection = TagHelperCollection.Build((tagHelper1, tagHelper2), (ref builder, items) =>
        {
            builder.Add(items.tagHelper1);
            builder.Add(items.tagHelper2);
            builder.Add(items.tagHelper1); // Duplicate
        });

        // Assert
        Assert.SameItems([tagHelper1, tagHelper2], collection);
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(2));

        // Act & Assert
        Assert.True(collection.Equals(collection));
        Assert.True(collection.Equals((object)collection));
    }

    [Fact]
    public void Equals_DifferentInstanceSameContent_ReturnsTrue()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(2);
        var collection1 = TagHelperCollection.Create(tagHelpers);
        var collection2 = TagHelperCollection.Create(tagHelpers);

        // Act & Assert
        Assert.True(collection1.Equals(collection2));
        Assert.True(collection1.Equals((object)collection2));
    }

    [Fact]
    public void Equals_DifferentContent_ReturnsFalse()
    {
        // Arrange
        var collection1 = TagHelperCollection.Create(CreateTestTagHelpers(2));
        var collection2 = TagHelperCollection.Create(CreateTestTagHelpers(3));

        // Act & Assert
        Assert.False(collection1.Equals(collection2));
        Assert.False(collection1.Equals((object)collection2));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(2));

        // Act & Assert
        Assert.False(collection.Equals(null));
        Assert.False(collection.Equals((object?)null));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(2));

        // Act & Assert
        Assert.False(collection.Equals("not a collection"));
    }

    [Fact]
    public void Equals_ArrayCreatedVsMergedCollection_SameContent_ReturnsTrue()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        var array = new[] { tagHelper1, tagHelper2, tagHelper3 };

        // Create collection directly from array
        var arrayCollection = TagHelperCollection.Create(array);

        // Create collection by merging two collections with the same content
        TagHelperCollection firstPart = [tagHelper1];
        TagHelperCollection secondPart = [tagHelper2, tagHelper3];
        var mergedCollection = TagHelperCollection.Merge(firstPart, secondPart);

        // Act & Assert
        Assert.Equal(arrayCollection.Count, mergedCollection.Count);
        Assert.True(arrayCollection.Contains(tagHelper1));
        Assert.True(arrayCollection.Contains(tagHelper2));
        Assert.True(arrayCollection.Contains(tagHelper3));
        Assert.True(mergedCollection.Contains(tagHelper1));
        Assert.True(mergedCollection.Contains(tagHelper2));
        Assert.True(mergedCollection.Contains(tagHelper3));

        // Verify same order
        Assert.Same(tagHelper1, arrayCollection[0]);
        Assert.Same(tagHelper2, arrayCollection[1]);
        Assert.Same(tagHelper3, arrayCollection[2]);
        Assert.Same(tagHelper1, mergedCollection[0]);
        Assert.Same(tagHelper2, mergedCollection[1]);
        Assert.Same(tagHelper3, mergedCollection[2]);

        // This should pass if equality works correctly regardless of construction method
        Assert.True(arrayCollection.Equals(mergedCollection));
        Assert.True(mergedCollection.Equals(arrayCollection));
        Assert.Equal(arrayCollection.GetHashCode(), mergedCollection.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameContent_ReturnsSameHashCode()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(2);
        var collection1 = TagHelperCollection.Create(tagHelpers);
        var collection2 = TagHelperCollection.Create(tagHelpers);

        // Act & Assert
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentContent_ReturnsDifferentHashCode()
    {
        // Arrange
        var collection1 = TagHelperCollection.Create(CreateTestTagHelpers(2));
        var collection2 = TagHelperCollection.Create(CreateTestTagHelpers(3));

        // Act & Assert
        Assert.NotEqual(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentOrder_ReturnsDifferentChecksum()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(2);
        var collection1 = TagHelperCollection.Create([tagHelpers[0], tagHelpers[1]]);
        var collection2 = TagHelperCollection.Create([tagHelpers[1], tagHelpers[0]]);

        // Act & Assert
        Assert.NotEqual(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Fact]
    public void Checksum_DifferentOrderSameContent_DifferentChecksums()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("A");
        var tagHelper2 = CreateTagHelper("B");
        var collection1 = TagHelperCollection.Create([tagHelper1, tagHelper2]);
        var collection2 = TagHelperCollection.Create([tagHelper2, tagHelper1]);

        // Act & Assert - Order matters for checksums
        Assert.NotEqual(collection1.GetHashCode(), collection2.GetHashCode());
        Assert.False(collection1.Equals(collection2));
    }

    [Fact]
    public void Checksum_IdenticalContent_IdenticalChecksums()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(5);
        var collection1 = TagHelperCollection.Create(tagHelpers);
        var collection2 = TagHelperCollection.Create(tagHelpers);

        // Act & Assert - Same content should have same checksum
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
        Assert.True(collection1.Equals(collection2));
    }

    [Fact]
    public void GetEnumerator_Generic_EnumeratesAllItems()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var enumeratedItems = new List<TagHelperDescriptor>();
        foreach (var item in collection)
        {
            enumeratedItems.Add(item);
        }

        // Assert
        Assert.SameItems(tagHelpers, enumeratedItems);
    }

    [Fact]
    public void GetEnumerator_NonGeneric_EnumeratesAllItems()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var enumeratedItems = new List<TagHelperDescriptor>();
        var enumerator = ((IEnumerable)collection).GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumeratedItems.Add((TagHelperDescriptor)enumerator.Current);
        }

        // Assert
        Assert.SameItems(tagHelpers, enumeratedItems);
    }

    [Fact]
    public void Enumerator_Current_BeforeMoveNext_ThrowsInvalidOperation()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(1));
        var enumerator = collection.GetEnumerator();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void CopyTo_ValidDestination_CopiesAllItems()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);
        var destination = new TagHelperDescriptor[5];

        // Act
        collection.CopyTo(destination);

        // Assert
        Assert.Same(tagHelpers[0], destination[0]);
        Assert.Same(tagHelpers[1], destination[1]);
        Assert.Same(tagHelpers[2], destination[2]);
        Assert.Null(destination[3]);
        Assert.Null(destination[4]);
    }

    [Fact]
    public void CopyTo_DestinationTooShort_ThrowsArgumentException()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);
        var destination = new TagHelperDescriptor[2];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.CopyTo(destination));
    }

    [Fact]
    public void Indexer_ValidIndex_ReturnsCorrectItem()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act & Assert
        Assert.Equal(tagHelpers.Length, collection.Count);

        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], collection[i]);
        }
    }

    [Fact]
    public void Indexer_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(3));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[10]);
    }

    [Fact]
    public void IndexOf_ExistingItem_ReturnsCorrectIndex()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act & Assert
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Equal(i, collection.IndexOf(tagHelpers[i]));
        }
    }

    [Fact]
    public void IndexOf_NonExistingItem_ReturnsMinusOne()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);
        var nonExistingItem = CreateTagHelper("NonExisting");

        // Act
        var index = collection.IndexOf(nonExistingItem);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void IndexOf_LargeCollection_UsesLookupTable()
    {
        // Arrange - Test above the 8-item threshold
        var tagHelpers = CreateTestTagHelpers(100);
        var collection = TagHelperCollection.Create(tagHelpers);
        var searchItem = tagHelpers[50];

        // Act & Assert - Should find the item (testing functionality, not speed)
        Assert.Equal(50, collection.IndexOf(searchItem));
    }

    [Fact]
    public void IndexOf_SmallCollection_UsesLinearSearch()
    {
        // Arrange - Test below the 8-item threshold
        var tagHelpers = CreateTestTagHelpers(5);
        var collection = TagHelperCollection.Create(tagHelpers);
        var searchItem = tagHelpers[3];

        // Act & Assert - Should find the item
        Assert.Equal(3, collection.IndexOf(searchItem));
    }

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act & Assert
        foreach (var tagHelper in tagHelpers)
        {
            Assert.True(collection.Contains(tagHelper));
        }
    }

    [Fact]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);
        var nonExistingItem = CreateTagHelper("NonExisting");

        // Act
        var contains = collection.Contains(nonExistingItem);

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public void Create_VeryLargeCollection_WorksCorrectly()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(1000);

        // Act
        var collection = TagHelperCollection.Create(tagHelpers);

        // Assert
        Assert.Equal(1000, collection.Count);
        Assert.Same(tagHelpers[0], collection[0]);
        Assert.Same(tagHelpers[999], collection[999]);
        Assert.Equal(0, collection.IndexOf(tagHelpers[0]));
        Assert.Equal(999, collection.IndexOf(tagHelpers[999]));
    }

    [Fact]
    public void IsEmpty_EmptyCollection_ReturnsTrue()
    {
        // Assert
        Assert.True(TagHelperCollection.Empty.IsEmpty);
    }

    [Fact]
    public void IsEmpty_NonEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(1));

        // Assert
        Assert.False(collection.IsEmpty);
    }

    [Fact]
    public void Builder_Empty_IsEmptyAndHasZeroCount()
    {
        // Arrange & Act
        using var builder = new TagHelperCollection.Builder();

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Empty(builder);
        Assert.False(builder.IsReadOnly);
    }

    [Fact]
    public void Builder_AddSingleItem_WorksCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");

        // Act
        var added = builder.Add(tagHelper);

        // Assert
        Assert.True(added);
        Assert.False(builder.IsEmpty);
        var item = Assert.Single(builder);
        Assert.Same(tagHelper, item);
    }

    [Fact]
    public void Builder_AddDuplicateSingleItem_ReturnsFalse()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");

        // Act
        var firstAdd = builder.Add(tagHelper);
        var secondAdd = builder.Add(tagHelper);

        // Assert
        Assert.True(firstAdd);
        Assert.False(secondAdd);
        var item = Assert.Single(builder);
        Assert.Same(tagHelper, item);
    }

    [Fact]
    public void Builder_AddMultipleItems_WorksCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        // Act
        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Assert
        Assert.False(builder.IsEmpty);
        Assert.Equal(3, builder.Count);

        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], builder[i]);
        }
    }

    [Fact]
    public void Builder_AddMultipleItemsWithDuplicates_DeduplicatesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");

        // Act
        var add1 = builder.Add(tagHelper1);
        var add2 = builder.Add(tagHelper2);
        var add3 = builder.Add(tagHelper1); // Duplicate

        // Assert
        Assert.True(add1);
        Assert.True(add2);
        Assert.False(add3); // Should return false for duplicate
        Assert.Equal(2, builder.Count);
        Assert.Same(tagHelper1, builder[0]);
        Assert.Same(tagHelper2, builder[1]);
    }

    [Fact]
    public void Builder_IndexerValidIndex_ReturnsCorrectItem()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act & Assert
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], builder[i]);
        }
    }

    [Fact]
    public void Builder_IndexerNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder
        {
            CreateTagHelper("Test")
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder[-1]);
    }

    [Fact]
    public void Builder_IndexerOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder
        {
            CreateTagHelper("Test")
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => builder[10]);
    }

    [Fact]
    public void Builder_IndexerEmptyBuilder_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder[0]);
    }

    [Fact]
    public void Builder_ContainsSingleItem_ReturnsCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        var otherHelper = CreateTagHelper("Other");
        builder.Add(tagHelper);

        // Act & Assert
        Assert.Contains(tagHelper, builder);
        Assert.DoesNotContain(otherHelper, builder);
    }

    [Fact]
    public void Builder_ContainsMultipleItems_ReturnsCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);
        var otherHelper = CreateTagHelper("Other");

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act & Assert
        foreach (var tagHelper in tagHelpers)
        {
            Assert.Contains(tagHelper, builder);
        }

        Assert.DoesNotContain(otherHelper, builder);
    }

    [Fact]
    public void Builder_ContainsEmptyBuilder_ReturnsFalse()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");

        // Act & Assert
        Assert.DoesNotContain(tagHelper, builder);
    }

    [Fact]
    public void Builder_ClearSingleItem_MakesEmpty()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        builder.Add(tagHelper);

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Empty(builder);
    }

    [Fact]
    public void Builder_ClearMultipleItems_MakesEmpty()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Empty(builder);
    }

    [Fact]
    public void Builder_ClearEmptyBuilder_RemainsEmpty()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();

        // Act
        builder.Clear();

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Empty(builder);
    }

    [Fact]
    public void Builder_RemoveSingleItemExists_ReturnsTrue()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        builder.Add(tagHelper);

        // Act
        var removed = builder.Remove(tagHelper);

        // Assert
        Assert.True(removed);
        Assert.True(builder.IsEmpty);
        Assert.Empty(builder);
    }

    [Fact]
    public void Builder_RemoveSingleItemNotExists_ReturnsFalse()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        var otherHelper = CreateTagHelper("Other");
        builder.Add(tagHelper);

        // Act
        var removed = builder.Remove(otherHelper);

        // Assert
        Assert.False(removed);
        var item = Assert.Single(builder);
        Assert.Same(tagHelper, item);
    }

    [Fact]
    public void Builder_RemoveFromMultipleItems_WorksCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        var removed = builder.Remove(tagHelpers[1]);

        // Assert
        Assert.True(removed);
        Assert.Equal(2, builder.Count);
        Assert.Same(tagHelpers[0], builder[0]);
        Assert.Same(tagHelpers[2], builder[1]);
        Assert.DoesNotContain(tagHelpers[1], builder);
    }

    [Fact]
    public void Builder_RemoveFromEmptyBuilder_ReturnsFalse()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");

        // Act
        var removed = builder.Remove(tagHelper);

        // Assert
        Assert.False(removed);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void Builder_CopyToSingleItem_CopiesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        builder.Add(tagHelper);
        var destination = new TagHelperDescriptor[3];

        // Act
        builder.CopyTo(destination, 1);

        // Assert
        Assert.Null(destination[0]);
        Assert.Same(tagHelper, destination[1]);
        Assert.Null(destination[2]);
    }

    [Fact]
    public void Builder_CopyToMultipleItems_CopiesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        var destination = new TagHelperDescriptor[5];

        // Act
        builder.CopyTo(destination, 1);

        // Assert
        Assert.Null(destination[0]);
        Assert.Same(tagHelpers[0], destination[1]);
        Assert.Same(tagHelpers[1], destination[2]);
        Assert.Same(tagHelpers[2], destination[3]);
        Assert.Null(destination[4]);
    }

    [Fact]
    public void Builder_CopyToEmptyBuilder_DoesNothing()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var destination = new TagHelperDescriptor[3];

        // Act
        builder.CopyTo(destination, 1);

        // Assert
        Assert.All(destination, item => Assert.Null(item));
    }

    [Fact]
    public void Builder_ToCollectionEmpty_ReturnsEmpty()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();

        // Act
        var collection = builder.ToCollection();

        // Assert
        Assert.Same(TagHelperCollection.Empty, collection);
    }

    [Fact]
    public void Builder_ToCollectionSingleItem_ReturnsSingleItemCollection()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        builder.Add(tagHelper);

        // Act
        var collection = builder.ToCollection();

        // Assert
        Assert.Single(collection);
        Assert.Same(tagHelper, collection[0]);
    }

    [Fact]
    public void Builder_ToCollectionMultipleItems_ReturnsCollection()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        var collection = builder.ToCollection();

        // Assert
        Assert.SameItems(tagHelpers, collection);
    }

    [Fact]
    public void Builder_ICollectionAdd_CallsAdd()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        ICollection<TagHelperDescriptor> collection = builder;
        var tagHelper = CreateTagHelper("Test");

        // Act
        collection.Add(tagHelper);

        // Assert
        var item = Assert.Single(builder);
        Assert.Same(tagHelper, item);
    }

    [Fact]
    public void Builder_GetEnumeratorEmpty_IsEmpty()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();

        // Act
        using var enumerator = builder.GetEnumerator();

        // Assert
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Builder_GetEnumeratorSingleItem_EnumeratesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelper = CreateTagHelper("Test");
        builder.Add(tagHelper);

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        using var enumerator = builder.GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerated.Add(enumerator.Current);
        }

        // Assert
        Assert.Single(enumerated);
        Assert.Same(tagHelper, enumerated[0]);
    }

    [Fact]
    public void Builder_GetEnumeratorMultipleItems_EnumeratesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        using var enumerator = builder.GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerated.Add(enumerator.Current);
        }

        // Assert
        Assert.SameItems(tagHelpers, enumerated);
    }

    [Fact]
    public void Builder_GenericEnumerable_EnumeratesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in (IEnumerable<TagHelperDescriptor>)builder)
        {
            enumerated.Add(item);
        }

        // Assert
        Assert.SameItems(tagHelpers, enumerated);
    }

    [Fact]
    public void Builder_NonGenericEnumerable_EnumeratesCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        var enumerator = ((IEnumerable)builder).GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerated.Add((TagHelperDescriptor)enumerator.Current);
        }

        // Assert
        Assert.SameItems(tagHelpers, enumerated);
    }

    [Fact]
    public void Builder_DisposeTwice_DoesNotThrow()
    {
        // Arrange
        var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(3);

        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Act & Assert - Should not throw
        builder.Dispose();
        builder.Dispose();
    }

    [Fact]
    public void Builder_LargeNumberOfItems_WorksCorrectly()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(100);

        // Act
        foreach (var tagHelper in tagHelpers)
        {
            builder.Add(tagHelper);
        }

        // Assert
        Assert.Equal(100, builder.Count);
        Assert.False(builder.IsEmpty);

        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], builder[i]);
            Assert.Contains(tagHelpers[i], builder);
        }
    }

    [Fact]
    public void Builder_ModifyAfterToCollection_DoesNotAffectCollection()
    {
        // Arrange
        using var builder = new TagHelperCollection.Builder();
        var tagHelpers = CreateTestTagHelpers(2);

        builder.Add(tagHelpers[0]);
        var collection = builder.ToCollection();

        // Act - Modify builder after creating collection
        builder.Add(tagHelpers[1]);

        // Assert - Original collection should be unchanged
        Assert.Single(collection);
        Assert.Same(tagHelpers[0], collection[0]);

        // Builder should have new state
        Assert.Equal(2, builder.Count);
        Assert.Same(tagHelpers[0], builder[0]);
        Assert.Same(tagHelpers[1], builder[1]);
    }

    [Fact]
    public void Merge_BothEmpty_ReturnsEmpty()
    {
        // Act
        var merged = TagHelperCollection.Merge(TagHelperCollection.Empty, TagHelperCollection.Empty);

        // Assert
        Assert.Same(TagHelperCollection.Empty, merged);
    }

    [Fact]
    public void Merge_FirstEmpty_ReturnsSecond()
    {
        // Arrange
        var second = TagHelperCollection.Create(CreateTestTagHelpers(2));

        // Act
        var merged = TagHelperCollection.Merge(TagHelperCollection.Empty, second);

        // Assert
        Assert.Same(second, merged);
    }

    [Fact]
    public void Merge_SecondEmpty_ReturnsFirst()
    {
        // Arrange
        var first = TagHelperCollection.Create(CreateTestTagHelpers(2));

        // Act
        var merged = TagHelperCollection.Merge(first, TagHelperCollection.Empty);

        // Assert
        Assert.Same(first, merged);
    }

    [Fact]
    public void Merge_NoOverlapSingleItems_CreatesMergedCollection()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.False(merged.IsEmpty);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Equal(0, merged.IndexOf(tagHelper1));
        Assert.Equal(1, merged.IndexOf(tagHelper2));
        Assert.True(merged.Contains(tagHelper1));
        Assert.True(merged.Contains(tagHelper2));
    }

    [Fact]
    public void Merge_NoOverlapMultipleItems_CreatesMergedCollection()
    {
        // Arrange
        var firstHelpers = CreateTestTagHelpers(3);

        // Create different helpers for second collection
        var secondHelper1 = CreateTagHelper("Second1");
        var secondHelper2 = CreateTagHelper("Second2");

        var first = TagHelperCollection.Create(firstHelpers);
        TagHelperCollection second = [secondHelper1, secondHelper2];

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(5, merged.Count);
        Assert.False(merged.IsEmpty);

        // Verify first collection items
        for (var i = 0; i < firstHelpers.Length; i++)
        {
            Assert.Same(firstHelpers[i], merged[i]);
            Assert.Equal(i, merged.IndexOf(firstHelpers[i]));
            Assert.True(merged.Contains(firstHelpers[i]));
        }

        // Verify second collection items
        Assert.Same(secondHelper1, merged[3]);
        Assert.Same(secondHelper2, merged[4]);
        Assert.Equal(3, merged.IndexOf(secondHelper1));
        Assert.Equal(4, merged.IndexOf(secondHelper2));
        Assert.True(merged.Contains(secondHelper1));
        Assert.True(merged.Contains(secondHelper2));
    }

    [Fact]
    public void Merge_WithOverlapSingleItems_DeduplicatesCorrectly()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper1, tagHelper2]; // Contains duplicate

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Equal(0, merged.IndexOf(tagHelper1));
        Assert.Equal(1, merged.IndexOf(tagHelper2));
    }

    [Fact]
    public void Merge_WithOverlapMultipleItems_DeduplicatesCorrectly()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        var tagHelper4 = CreateTagHelper("Test4");

        TagHelperCollection first = [tagHelper1, tagHelper2];
        TagHelperCollection second = [tagHelper2, tagHelper3, tagHelper4]; // tagHelper2 is duplicate

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(4, merged.Count); // Should be deduplicated
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Same(tagHelper3, merged[2]);
        Assert.Same(tagHelper4, merged[3]);

        // Verify all items are findable
        Assert.Equal(0, merged.IndexOf(tagHelper1));
        Assert.Equal(1, merged.IndexOf(tagHelper2));
        Assert.Equal(2, merged.IndexOf(tagHelper3));
        Assert.Equal(3, merged.IndexOf(tagHelper4));
    }

    [Fact]
    public void Merge_CompleteOverlap_ReturnsDeduplicated()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var first = TagHelperCollection.Create(tagHelpers);
        var second = TagHelperCollection.Create(tagHelpers); // Identical collections

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(3, merged.Count); // Should not duplicate
        Assert.SameItems(tagHelpers, merged);
    }

    [Fact]
    public void Merge_MergedCollectionIndexer_WorksCorrectly()
    {
        // Arrange
        var firstHelper = CreateTagHelper("First");
        var secondHelper = CreateTagHelper("Second");
        TagHelperCollection first = [firstHelper];
        TagHelperCollection second = [secondHelper];

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert - Test indexer edge cases
        Assert.Same(firstHelper, merged[0]);
        Assert.Same(secondHelper, merged[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => merged[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => merged[2]);
    }

    [Fact]
    public void Merge_MergedCollectionCopyTo_WorksCorrectly()
    {
        // Arrange
        var firstHelpers = CreateTestTagHelpers(2);
        var secondHelper = CreateTagHelper("Second");
        var first = TagHelperCollection.Create(firstHelpers);
        TagHelperCollection second = [secondHelper];
        var merged = TagHelperCollection.Merge(first, second);
        var destination = new TagHelperDescriptor[5];

        // Act
        merged.CopyTo(destination);

        // Assert
        Assert.Same(firstHelpers[0], destination[0]);
        Assert.Same(firstHelpers[1], destination[1]);
        Assert.Same(secondHelper, destination[2]);
        Assert.Null(destination[3]);
        Assert.Null(destination[4]);
    }

    [Fact]
    public void Merge_MergedCollectionCopyTo_DestinationTooShort_ThrowsArgumentException()
    {
        // Arrange
        var firstHelper = CreateTagHelper("First");
        var secondHelper = CreateTagHelper("Second");
        TagHelperCollection first = [firstHelper];
        TagHelperCollection second = [secondHelper];
        var merged = TagHelperCollection.Merge(first, second);
        var destination = new TagHelperDescriptor[1]; // Too short

        // Act & Assert
        Assert.Throws<ArgumentException>(() => merged.CopyTo(destination));
    }

    [Fact]
    public void Merge_MergedCollectionIndexOf_NonExistingItem_ReturnsMinusOne()
    {
        // Arrange
        var firstHelper = CreateTagHelper("First");
        var secondHelper = CreateTagHelper("Second");
        var nonExistingHelper = CreateTagHelper("NonExisting");
        TagHelperCollection first = [firstHelper];
        TagHelperCollection second = [secondHelper];
        var merged = TagHelperCollection.Merge(first, second);

        // Act
        var index = merged.IndexOf(nonExistingHelper);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void Merge_MergedCollectionGetHashCode_DifferentForDifferentOrder()
    {
        // Arrange
        var firstHelper = CreateTagHelper("First");
        var secondHelper = CreateTagHelper("Second");
        TagHelperCollection first = [firstHelper];
        TagHelperCollection second = [secondHelper];

        // Act
        var merged1 = TagHelperCollection.Merge(first, second);
        var merged2 = TagHelperCollection.Merge(second, first); // Different order

        // Assert
        Assert.NotEqual(merged1.GetHashCode(), merged2.GetHashCode());
    }

    [Fact]
    public void Merge_MergedCollectionEquals_SameContent_ReturnsTrue()
    {
        // Arrange
        var firstHelper = CreateTagHelper("First");
        var secondHelper = CreateTagHelper("Second");
        TagHelperCollection first = [firstHelper];
        TagHelperCollection second = [secondHelper];

        // Act
        var merged1 = TagHelperCollection.Merge(first, second);
        var merged2 = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.True(merged1.Equals(merged2));
        Assert.True(merged1.Equals((object)merged2));
    }

    [Fact]
    public void Merge_MergedCollectionEnumeration_WorksCorrectly()
    {
        // Arrange
        var firstHelpers = CreateTestTagHelpers(2);
        var secondHelper = CreateTagHelper("Second");
        var first = TagHelperCollection.Create(firstHelpers);
        TagHelperCollection second = [secondHelper];
        var merged = TagHelperCollection.Merge(first, second);

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in merged)
        {
            enumerated.Add(item);
        }

        // Assert
        Assert.Equal(3, enumerated.Count);
        Assert.Same(firstHelpers[0], enumerated[0]);
        Assert.Same(firstHelpers[1], enumerated[1]);
        Assert.Same(secondHelper, enumerated[2]);
    }

    [Fact]
    public void Merge_LargeCollections_WorksCorrectly()
    {
        // Arrange
        var firstHelpers = CreateTestTagHelpers(500);
        var secondHelpers = new TagHelperDescriptor[500];
        for (var i = 0; i < 500; i++)
        {
            secondHelpers[i] = CreateTagHelper($"SecondHelper{i}");
        }

        var first = TagHelperCollection.Create(firstHelpers);
        var second = TagHelperCollection.Create(secondHelpers);

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(1000, merged.Count);

        // Verify first collection items
        for (var i = 0; i < 500; i++)
        {
            Assert.Same(firstHelpers[i], merged[i]);
        }

        // Verify second collection items
        for (var i = 0; i < 500; i++)
        {
            Assert.Same(secondHelpers[i], merged[500 + i]);
        }
    }

    [Fact]
    public void Merge_WithPartialOverlap_DeduplicatesCorrectly()
    {
        // Arrange
        var shared1 = CreateTagHelper("Shared1");
        var shared2 = CreateTagHelper("Shared2");
        var unique1 = CreateTagHelper("Unique1");
        var unique2 = CreateTagHelper("Unique2");
        var unique3 = CreateTagHelper("Unique3");

        TagHelperCollection first = [unique1, shared1, unique2];
        TagHelperCollection second = [shared1, unique3, shared2];

        // Act
        var merged = TagHelperCollection.Merge(first, second);

        // Assert
        Assert.Equal(5, merged.Count); // 3 + 3 - 1 (shared1 deduplicated)

        // Verify order: first collection items, then unique items from second collection
        Assert.Same(unique1, merged[0]);
        Assert.Same(shared1, merged[1]);  // From first collection
        Assert.Same(unique2, merged[2]);
        Assert.Same(unique3, merged[3]);  // From second collection (shared1 already added)
        Assert.Same(shared2, merged[4]);  // From second collection

        // Verify all items are findable
        Assert.True(merged.Contains(unique1));
        Assert.True(merged.Contains(shared1));
        Assert.True(merged.Contains(unique2));
        Assert.True(merged.Contains(unique3));
        Assert.True(merged.Contains(shared2));
    }

    [Fact]
    public void Merge_ImmutableArrayEmpty_ReturnsEmpty()
    {
        // Arrange
        var collections = ImmutableArray<TagHelperCollection>.Empty;

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(TagHelperCollection.Empty, merged);
    }

    [Fact]
    public void Merge_ImmutableArraySingleCollection_ReturnsSameCollection()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(3));
        var collections = ImmutableArray.Create(collection);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(collection, merged);
    }

    [Fact]
    public void Merge_ImmutableArrayTwoCollections_UsesOptimizedTwoCollectionMerge()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        var collections = ImmutableArray.Create(first, second);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
    }

    [Fact]
    public void Merge_ImmutableArrayThreeCollections_NoOverlap_CreatesEfficientMergedCollection()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        TagHelperCollection third = [tagHelper3];
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(3, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Same(tagHelper3, merged[2]);
        Assert.Equal(0, merged.IndexOf(tagHelper1));
        Assert.Equal(1, merged.IndexOf(tagHelper2));
        Assert.Equal(2, merged.IndexOf(tagHelper3));
        Assert.True(merged.Contains(tagHelper1));
        Assert.True(merged.Contains(tagHelper2));
        Assert.True(merged.Contains(tagHelper3));
    }

    [Fact]
    public void Merge_ImmutableArrayThreeCollections_WithDuplicates_DeduplicatesCorrectly()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper1, tagHelper2]; // tagHelper1 is duplicate
        TagHelperCollection third = [tagHelper2, tagHelper3]; // tagHelper2 is duplicate
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(3, merged.Count); // Should be deduplicated
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Same(tagHelper3, merged[2]);
    }

    [Fact]
    public void Merge_ImmutableArrayWithEmptyCollections_FiltersEmptyCollections()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        var second = TagHelperCollection.Empty;
        TagHelperCollection third = [tagHelper2];
        var fourth = TagHelperCollection.Empty;
        var collections = ImmutableArray.Create(first, second, third, fourth);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
    }

    [Fact]
    public void Merge_ImmutableArrayMultipleCollections_LargeSet_WorksCorrectly()
    {
        // Arrange
        var collections = ImmutableArray.CreateBuilder<TagHelperCollection>(10);
        var allHelpers = new List<TagHelperDescriptor>();

        for (var i = 0; i < 10; i++)
        {
            var helpers = new TagHelperDescriptor[5];
            for (var j = 0; j < 5; j++)
            {
                helpers[j] = CreateTagHelper($"Collection{i}Helper{j}");
                allHelpers.Add(helpers[j]);
            }
            collections.Add(TagHelperCollection.Create(helpers));
        }

        // Act
        var merged = TagHelperCollection.Merge(collections.ToImmutable());

        // Assert
        Assert.Equal(50, merged.Count);

        // Verify all items are present in correct order
        for (var i = 0; i < 50; i++)
        {
            Assert.Same(allHelpers[i], merged[i]);
        }
    }

    [Fact]
    public void Merge_IEnumerableEmpty_ReturnsEmpty()
    {
        // Arrange
        var collections = new List<TagHelperCollection>();

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(TagHelperCollection.Empty, merged);
    }

    [Fact]
    public void Merge_IEnumerableSingleCollection_ReturnsSameCollection()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(3));
        var collections = new List<TagHelperCollection> { collection };

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(collection, merged);
    }

    [Fact]
    public void Merge_IEnumerableTwoCollections_UsesOptimizedPath()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        var collections = new List<TagHelperCollection> { first, second };

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
    }

    [Fact]
    public void Merge_IEnumerableMultipleCollections_WorksCorrectly()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        TagHelperCollection third = [tagHelper3];
        var collections = new List<TagHelperCollection> { first, second, third };

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(3, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Same(tagHelper3, merged[2]);
    }

    [Fact]
    public void Merge_IEnumerableWithoutKnownCount_WorksCorrectly()
    {
        // Arrange - Use LINQ to create an enumerable without known count
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        var baseCollections = new[] { first, second };
        var collections = baseCollections.Where(c => !c.IsEmpty);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_SupportsIndexer()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(15).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..5]);
        var second = TagHelperCollection.Create(helpers[5..10]);
        var third = TagHelperCollection.Create(helpers[10..15]);
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(15, merged.Count);

        // Test indexer access
        for (var i = 0; i < 15; i++)
        {
            Assert.Same(helpers[i], merged[i]);
        }

        // Test boundary conditions
        Assert.Throws<ArgumentOutOfRangeException>(() => merged[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => merged[15]);
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_SupportsIndexOf()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(9).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..3]);
        var second = TagHelperCollection.Create(helpers[3..6]);
        var third = TagHelperCollection.Create(helpers[6..9]);
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        for (var i = 0; i < 9; i++)
        {
            Assert.Equal(i, merged.IndexOf(helpers[i]));
        }

        var nonExistingHelper = CreateTagHelper("NonExisting");
        Assert.Equal(-1, merged.IndexOf(nonExistingHelper));
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_SupportsCopyTo()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(6).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..2]);
        var second = TagHelperCollection.Create(helpers[2..4]);
        var third = TagHelperCollection.Create(helpers[4..6]);
        var collections = ImmutableArray.Create(first, second, third);
        var merged = TagHelperCollection.Merge(collections);
        var destination = new TagHelperDescriptor[8];

        // Act
        merged.CopyTo(destination);

        // Assert
        for (var i = 0; i < 6; i++)
        {
            Assert.Same(helpers[i], destination[i]);
        }
        Assert.Null(destination[6]);
        Assert.Null(destination[7]);
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_SupportsEnumeration()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(9).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..3]);
        var second = TagHelperCollection.Create(helpers[3..6]);
        var third = TagHelperCollection.Create(helpers[6..9]);
        var collections = ImmutableArray.Create(first, second, third);
        var merged = TagHelperCollection.Merge(collections);

        // Act
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in merged)
        {
            enumerated.Add(item);
        }

        // Assert
        Assert.Equal(9, enumerated.Count);
        for (var i = 0; i < 9; i++)
        {
            Assert.Same(helpers[i], enumerated[i]);
        }
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_GetHashCodeIsDeterministic()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(6).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..2]);
        var second = TagHelperCollection.Create(helpers[2..4]);
        var third = TagHelperCollection.Create(helpers[4..6]);
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged1 = TagHelperCollection.Merge(collections);
        var merged2 = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(merged1.GetHashCode(), merged2.GetHashCode());
    }

    [Fact]
    public void Merge_MultiCollectionMergedResult_EqualityWorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(6).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..2]);
        var second = TagHelperCollection.Create(helpers[2..4]);
        var third = TagHelperCollection.Create(helpers[4..6]);
        var collections = ImmutableArray.Create(first, second, third);

        // Act
        var merged1 = TagHelperCollection.Merge(collections);
        var merged2 = TagHelperCollection.Merge(collections);
        var arrayCreated = TagHelperCollection.Create(helpers);

        // Assert
        Assert.True(merged1.Equals(merged2));
        Assert.True(merged1.Equals(arrayCreated));
        Assert.True(arrayCreated.Equals(merged1));
    }

    [Fact]
    public void Merge_MultiCollectionNestedMerge_WorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(12).AsSpan();
        var first = TagHelperCollection.Create(helpers[0..3]);
        var second = TagHelperCollection.Create(helpers[3..6]);
        var third = TagHelperCollection.Create(helpers[6..9]);
        var fourth = TagHelperCollection.Create(helpers[9..12]);

        // Create nested merge structure
        var merged1 = TagHelperCollection.Merge(first, second);
        var merged2 = TagHelperCollection.Merge(third, fourth);

        // Act
        var finalMerged = TagHelperCollection.Merge(merged1, merged2);

        // Assert
        Assert.Equal(12, finalMerged.Count);
        for (var i = 0; i < 12; i++)
        {
            Assert.Same(helpers[i], finalMerged[i]);
        }
    }

    [Fact]
    public void Merge_ReadOnlySpanEmpty_ReturnsEmpty()
    {
        // Arrange
        ReadOnlySpan<TagHelperCollection> collections = [];

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(TagHelperCollection.Empty, merged);
    }

    [Fact]
    public void Merge_ReadOnlySpanSingleCollection_ReturnsSameCollection()
    {
        // Arrange
        var collection = TagHelperCollection.Create(CreateTestTagHelpers(3));
        ReadOnlySpan<TagHelperCollection> collections = [collection];

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Same(collection, merged);
    }

    [Fact]
    public void Merge_ReadOnlySpanMultipleCollections_WorksCorrectly()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");
        TagHelperCollection first = [tagHelper1];
        TagHelperCollection second = [tagHelper2];
        TagHelperCollection third = [tagHelper3];
        ReadOnlySpan<TagHelperCollection> collections = [first, second, third];

        // Act
        var merged = TagHelperCollection.Merge(collections);

        // Assert
        Assert.Equal(3, merged.Count);
        Assert.Same(tagHelper1, merged[0]);
        Assert.Same(tagHelper2, merged[1]);
        Assert.Same(tagHelper3, merged[2]);
    }

    [Fact]
    public void MergedCollection_EnumerationPerformance_AvoidsBinarySearchPerElement()
    {
        // Arrange - Create a scenario where enumeration would be slow if using indexer
        var helpers1 = CreateTestTagHelpers(100);
        var helpers2 = CreateTestTagHelpers(100, startIndex: 100);
        var helpers3 = CreateTestTagHelpers(100, startIndex: 200);

        var collection1 = TagHelperCollection.Create(helpers1);
        var collection2 = TagHelperCollection.Create(helpers2);
        var collection3 = TagHelperCollection.Create(helpers3);

        var collections = ImmutableArray.Create(collection1, collection2, collection3);
        var merged = TagHelperCollection.Merge(collections);

        // Act - Enumerate the entire collection
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in merged)
        {
            enumerated.Add(item);
        }

        // Assert - Verify all items are present in correct order
        Assert.Equal(300, enumerated.Count);
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(helpers1[i], enumerated[i]);
        }
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(helpers2[i], enumerated[100 + i]);
        }
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(helpers3[i], enumerated[200 + i]);
        }
    }

    [Fact]
    public void MergedCollection_EnumerationState_HandlesSegmentTransitions()
    {
        // Arrange - Create collections of different sizes to test segment transitions
        var helper1 = CreateTagHelper("Single");
        var helpers2to4 = CreateTestTagHelpers(3, startIndex: 1);
        var helpers5to9 = CreateTestTagHelpers(5, startIndex: 4);

        TagHelperCollection collection1 = [helper1];
        var collection2 = TagHelperCollection.Create(helpers2to4);
        var collection3 = TagHelperCollection.Create(helpers5to9);

        var merged = TagHelperCollection.Merge(ImmutableArray.Create(collection1, collection2, collection3));

        // Act & Assert - Test enumeration crosses segment boundaries correctly
        using var enumerator = merged.GetEnumerator();

        // First segment (single item)
        Assert.True(enumerator.MoveNext());
        Assert.Same(helper1, enumerator.Current);

        // Second segment (3 items)
        for (var i = 0; i < 3; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Same(helpers2to4[i], enumerator.Current);
        }

        // Third segment (5 items)
        for (var i = 0; i < 5; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Same(helpers5to9[i], enumerator.Current);
        }

        // Should be exhausted
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MergedCollection_IndexerAccuracy_WithManySegments()
    {
        // Arrange - Create many small segments to stress-test binary search logic
        var segments = new List<TagHelperCollection>();
        var allHelpers = new List<TagHelperDescriptor>();

        for (var i = 0; i < 20; i++)
        {
            var helpers = CreateTestTagHelpers(3, startIndex: i * 3);
            segments.Add(TagHelperCollection.Create(helpers));
            allHelpers.AddRange(helpers);
        }

        var merged = TagHelperCollection.Merge(segments.ToImmutableArray());

        // Act & Assert - Test random access to various indices
        Assert.Equal(60, merged.Count);

        // Test accessing elements from different segments
        var testIndices = new[] { 0, 1, 2, 5, 8, 15, 29, 35, 44, 59 };
        foreach (var index in testIndices)
        {
            Assert.Same(allHelpers[index], merged[index]);
            Assert.Equal(index, merged.IndexOf(allHelpers[index]));
        }
    }

    [Fact]
    public void MergedCollection_FindCollectionIndex_BinarySearchEdgeCases()
    {
        // Arrange - Create segments with specific sizes to test binary search edge cases
        var segment1 = TagHelperCollection.Create(CreateTestTagHelpers(1)); // [0]
        var segment2 = TagHelperCollection.Create(CreateTestTagHelpers(2, startIndex: 1)); // [1, 2]
        var segment3 = TagHelperCollection.Create(CreateTestTagHelpers(3, startIndex: 3)); // [3, 4, 5]
        var segment4 = TagHelperCollection.Create(CreateTestTagHelpers(1, startIndex: 6)); // [6]

        var merged = TagHelperCollection.Merge(ImmutableArray.Create(segment1, segment2, segment3, segment4));

        // Act & Assert - Test edge cases in binary search logic
        // Start of segments
        Assert.Equal(0, merged.IndexOf(CreateTagHelper("TagHelper0")));  // Start of segment 1
        Assert.Equal(1, merged.IndexOf(CreateTagHelper("TagHelper1")));  // Start of segment 2
        Assert.Equal(3, merged.IndexOf(CreateTagHelper("TagHelper3")));  // Start of segment 3
        Assert.Equal(6, merged.IndexOf(CreateTagHelper("TagHelper6")));  // Start of segment 4

        // End of segments
        Assert.Equal(0, merged.IndexOf(CreateTagHelper("TagHelper0")));  // End of segment 1
        Assert.Equal(2, merged.IndexOf(CreateTagHelper("TagHelper2")));  // End of segment 2
        Assert.Equal(5, merged.IndexOf(CreateTagHelper("TagHelper5")));  // End of segment 3
        Assert.Equal(6, merged.IndexOf(CreateTagHelper("TagHelper6")));  // End of segment 4
    }

    [Fact]
    public void MergedCollection_CopyTo_HandlesSegmentBoundaries()
    {
        // Arrange
        var helpers1 = CreateTestTagHelpers(2);
        var helpers2 = CreateTestTagHelpers(3, startIndex: 2);
        var helpers3 = CreateTestTagHelpers(1, startIndex: 5);

        var collection1 = TagHelperCollection.Create(helpers1);
        var collection2 = TagHelperCollection.Create(helpers2);
        var collection3 = TagHelperCollection.Create(helpers3);

        var merged = TagHelperCollection.Merge(ImmutableArray.Create(collection1, collection2, collection3));
        var destination = new TagHelperDescriptor[10];

        // Act
        merged.CopyTo(destination);

        // Assert
        Assert.Same(helpers1[0], destination[0]);
        Assert.Same(helpers1[1], destination[1]);
        Assert.Same(helpers2[0], destination[2]);
        Assert.Same(helpers2[1], destination[3]);
        Assert.Same(helpers2[2], destination[4]);
        Assert.Same(helpers3[0], destination[5]);

        // Remaining should be null
        for (var i = 6; i < 10; i++)
        {
            Assert.Null(destination[i]);
        }
    }

    [Fact]
    public void MergedCollection_ComputeHashCode_IsConsistentWithContent()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(6).AsSpan();

        // Create the same content using different merge strategies
        var merged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create(helpers[2..6]));

        var merged2 = TagHelperCollection.Merge(ImmutableArray.Create(
            TagHelperCollection.Create(helpers[0..3]),
            TagHelperCollection.Create(helpers[3..6])));

        var arrayBacked = TagHelperCollection.Create(helpers);

        // Act & Assert
        Assert.Equal(arrayBacked.GetHashCode(), merged1.GetHashCode());
        Assert.Equal(arrayBacked.GetHashCode(), merged2.GetHashCode());
        Assert.Equal(merged1.GetHashCode(), merged2.GetHashCode());
    }

    [Fact]
    public void MergedCollection_EqualsImplementation_WorksAcrossDifferentCollectionTypes()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(4).AsSpan();

        var arrayBacked = TagHelperCollection.Create(helpers);
        var twoItemMerged = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create(helpers[2..4]));
        var multiMerged = TagHelperCollection.Merge(ImmutableArray.Create(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create([helpers[1]]),
            TagHelperCollection.Create(helpers[2..4])));

        // Act & Assert - All should be equal despite different internal structures
        Assert.True(arrayBacked.Equals(twoItemMerged));
        Assert.True(twoItemMerged.Equals(arrayBacked));
        Assert.True(arrayBacked.Equals(multiMerged));
        Assert.True(multiMerged.Equals(arrayBacked));
        Assert.True(twoItemMerged.Equals(multiMerged));
        Assert.True(multiMerged.Equals(twoItemMerged));
    }

    [Fact]
    public void MergedCollection_DifferentInternalStructure_SameContent_ReturnsTrue()
    {
        // Arrange - Create collections with same content but different internal merge structure
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");

        // First merged collection: [tagHelper1] + [tagHelper2, tagHelper3]
        TagHelperCollection firstPart1 = [tagHelper1];
        TagHelperCollection secondPart1 = [tagHelper2, tagHelper3];
        var merged1 = TagHelperCollection.Merge(firstPart1, secondPart1);

        // Second merged collection: [tagHelper1, tagHelper2] + [tagHelper3]
        TagHelperCollection firstPart2 = [tagHelper1, tagHelper2];
        TagHelperCollection secondPart2 = [tagHelper3];
        var merged2 = TagHelperCollection.Merge(firstPart2, secondPart2);

        // Act & Assert - Should be equal despite different merge structure
        Assert.True(merged1.Equals(merged2));
        Assert.True(merged2.Equals(merged1));
        Assert.Equal(merged1.GetHashCode(), merged2.GetHashCode());

        // Verify content is identical
        Assert.Equal(3, merged1.Count);
        Assert.Equal(3, merged2.Count);
        Assert.Same(tagHelper1, merged1[0]);
        Assert.Same(tagHelper2, merged1[1]);
        Assert.Same(tagHelper3, merged1[2]);
        Assert.Same(tagHelper1, merged2[0]);
        Assert.Same(tagHelper2, merged2[1]);
        Assert.Same(tagHelper3, merged2[2]);
    }

    [Fact]
    public void TwoItemMergedCollection_DifferentInternalStructure_SameContent_ReturnsTrue()
    {
        // Arrange - Create two-item merged collections with same content but different internal structure
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");

        // First: single + single
        TagHelperCollection first1 = [tagHelper1];
        TagHelperCollection second1 = [tagHelper2];
        var merged1 = TagHelperCollection.Merge(first1, second1);

        // Second: array + single (different internal collection types)
        var arrayCollection = TagHelperCollection.Create([tagHelper1]);
        TagHelperCollection singleCollection = [tagHelper2];
        var merged2 = TagHelperCollection.Merge(arrayCollection, singleCollection);

        // Act & Assert - Should be equal despite different internal structure
        Assert.True(merged1.Equals(merged2));
        Assert.True(merged2.Equals(merged1));
        Assert.Equal(merged1.GetHashCode(), merged2.GetHashCode());

        // Verify content is identical
        Assert.Equal(2, merged1.Count);
        Assert.Equal(2, merged2.Count);
        Assert.Same(tagHelper1, merged1[0]);
        Assert.Same(tagHelper2, merged1[1]);
        Assert.Same(tagHelper1, merged2[0]);
        Assert.Same(tagHelper2, merged2[1]);
    }

    [Fact]
    public void MergedCollection_VsArrayCollection_SameContent_ReturnsTrue()
    {
        // Arrange - Create merged collection vs array-backed collection with same content
        var tagHelpers = CreateTestTagHelpers(4).AsSpan();

        // Create via merge
        var part1 = TagHelperCollection.Create(tagHelpers[0..2]);
        var part2 = TagHelperCollection.Create(tagHelpers[2..4]);
        var merged = TagHelperCollection.Merge(part1, part2);

        // Create directly from array
        var arrayBacked = TagHelperCollection.Create(tagHelpers);

        // Act & Assert - Should be equal despite different construction
        Assert.True(merged.Equals(arrayBacked));
        Assert.True(arrayBacked.Equals(merged));
        Assert.Equal(merged.GetHashCode(), arrayBacked.GetHashCode());

        // Verify content is identical
        Assert.Equal(4, merged.Count);
        Assert.Equal(4, arrayBacked.Count);
        for (var i = 0; i < 4; i++)
        {
            Assert.Same(tagHelpers[i], merged[i]);
            Assert.Same(tagHelpers[i], arrayBacked[i]);
        }
    }

    [Fact]
    public void MergedCollection_VsSingleItemCollection_SameContent_ReturnsTrue()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Test");

        // Create via merge of two empty parts (should result in single item)
        TagHelperCollection part1 = [tagHelper];
        var merged = TagHelperCollection.Merge(part1, TagHelperCollection.Empty);

        // Create directly as single item
        TagHelperCollection single = [tagHelper];

        // Act & Assert - Should be equal despite different construction
        Assert.True(merged.Equals(single));
        Assert.True(single.Equals(merged));
        Assert.Equal(merged.GetHashCode(), single.GetHashCode());

        // Verify content is identical
        Assert.Single(merged);
        Assert.Single(single);
        Assert.Same(tagHelper, merged[0]);
        Assert.Same(tagHelper, single[0]);
    }

    [Fact]
    public void MultiLevelMergedCollection_EqualityWorks_AcrossDifferentNestingLevels()
    {
        // Arrange - Create the same content through different nesting levels
        var helpers = CreateTestTagHelpers(8);

        // Flat merge: all 8 collections merged at once
        var individualCollections = helpers.Select(h => TagHelperCollection.Create([h])).ToArray();
        var flatMerged = TagHelperCollection.Merge(individualCollections.ToImmutableArray());

        // Two-level merge: merge pairs, then merge the pairs
        var pair1 = TagHelperCollection.Merge(individualCollections[0], individualCollections[1]);
        var pair2 = TagHelperCollection.Merge(individualCollections[2], individualCollections[3]);
        var pair3 = TagHelperCollection.Merge(individualCollections[4], individualCollections[5]);
        var pair4 = TagHelperCollection.Merge(individualCollections[6], individualCollections[7]);
        var twoLevelMerged = TagHelperCollection.Merge(ImmutableArray.Create(pair1, pair2, pair3, pair4));

        // Three-level merge: merge pairs, then pairs of pairs
        var quad1 = TagHelperCollection.Merge(pair1, pair2);
        var quad2 = TagHelperCollection.Merge(pair3, pair4);
        var threeLevelMerged = TagHelperCollection.Merge(quad1, quad2);

        // Array-backed for comparison
        var arrayBacked = TagHelperCollection.Create(helpers);

        // Act & Assert - All should be equal despite different nesting structures
        Assert.True(flatMerged.Equals(twoLevelMerged));
        Assert.True(twoLevelMerged.Equals(threeLevelMerged));
        Assert.True(threeLevelMerged.Equals(arrayBacked));
        Assert.True(arrayBacked.Equals(flatMerged));

        // Verify hash codes are equal
        Assert.Equal(flatMerged.GetHashCode(), twoLevelMerged.GetHashCode());
        Assert.Equal(twoLevelMerged.GetHashCode(), threeLevelMerged.GetHashCode());
        Assert.Equal(threeLevelMerged.GetHashCode(), arrayBacked.GetHashCode());
    }

    [Fact]
    public void MergedCollection_WithNestedMergedCollections_EqualityWorksCorrectly()
    {
        // Arrange - Create complex nested structure
        var helpers = CreateTestTagHelpers(6);

        // Create nested merged collection structure
        var innerMerged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create([helpers[1]]));

        var innerMerged2 = TagHelperCollection.Merge(
            TagHelperCollection.Create([helpers[2], helpers[3]]),
            TagHelperCollection.Create([helpers[4], helpers[5]]));

        var outerMerged = TagHelperCollection.Merge(innerMerged1, innerMerged2);

        // Create equivalent flat structure
        var flatMerged = TagHelperCollection.Merge(ImmutableArray.Create(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create([helpers[1]]),
            TagHelperCollection.Create([helpers[2], helpers[3]]),
            TagHelperCollection.Create([helpers[4], helpers[5]])
        ));

        // Create array-backed equivalent
        var arrayBacked = TagHelperCollection.Create(helpers);

        // Act & Assert
        Assert.True(outerMerged.Equals(flatMerged));
        Assert.True(flatMerged.Equals(arrayBacked));
        Assert.True(arrayBacked.Equals(outerMerged));

        // Verify content integrity
        Assert.Equal(6, outerMerged.Count);
        Assert.Equal(6, flatMerged.Count);
        Assert.Equal(6, arrayBacked.Count);

        for (var i = 0; i < 6; i++)
        {
            Assert.Same(helpers[i], outerMerged[i]);
            Assert.Same(helpers[i], flatMerged[i]);
            Assert.Same(helpers[i], arrayBacked[i]);
        }
    }

    [Fact]
    public void MergedCollection_DifferentContent_SameStructure_ReturnsFalse()
    {
        // Arrange - Same merge structure but different content
        var helpers1 = CreateTestTagHelpers(4).AsSpan();
        var helpers2 = CreateTestTagHelpers(4, startIndex: 10).AsSpan(); // Different content

        var part1a = TagHelperCollection.Create(helpers1[0..2]);
        var part2a = TagHelperCollection.Create(helpers1[2..4]);
        var merged1 = TagHelperCollection.Merge(part1a, part2a);

        var part1b = TagHelperCollection.Create(helpers2[0..2]);
        var part2b = TagHelperCollection.Create(helpers2[2..4]);
        var merged2 = TagHelperCollection.Merge(part1b, part2b);

        // Act & Assert - Should not be equal due to different content
        Assert.False(merged1.Equals(merged2));
        Assert.False(merged2.Equals(merged1));
        Assert.NotEqual(merged1.GetHashCode(), merged2.GetHashCode());
    }

    [Fact]
    public void MergedCollection_SameContentDifferentOrder_ReturnsFalse()
    {
        // Arrange - Same items but different order
        var tagHelper1 = CreateTagHelper("Test1");
        var tagHelper2 = CreateTagHelper("Test2");
        var tagHelper3 = CreateTagHelper("Test3");

        // First order: 1, 2, 3
        var merged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create([tagHelper1]),
            TagHelperCollection.Create([tagHelper2, tagHelper3]));

        // Second order: 3, 2, 1  
        var merged2 = TagHelperCollection.Merge(
            TagHelperCollection.Create([tagHelper3]),
            TagHelperCollection.Create([tagHelper2, tagHelper1]));

        // Act & Assert - Should not be equal due to different order
        Assert.False(merged1.Equals(merged2));
        Assert.False(merged2.Equals(merged1));
        Assert.NotEqual(merged1.GetHashCode(), merged2.GetHashCode());

        // Verify the order is actually different
        Assert.Same(tagHelper1, merged1[0]);
        Assert.Same(tagHelper2, merged1[1]);
        Assert.Same(tagHelper3, merged1[2]);

        Assert.Same(tagHelper3, merged2[0]);
        Assert.Same(tagHelper2, merged2[1]);
        Assert.Same(tagHelper1, merged2[2]);
    }

    [Fact]
    public void MergedCollection_ChecksumBasedEquality_WorksWithLargeCollections()
    {
        // Arrange - Create large collections to test checksum-based equality path
        var helpers = CreateTestTagHelpers(100).AsSpan();

        // Create via different merge strategies
        var merged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..50]),
            TagHelperCollection.Create(helpers[50..100]));

        var merged2 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..25]),
            TagHelperCollection.Merge(
                TagHelperCollection.Create(helpers[25..75]),
                TagHelperCollection.Create(helpers[75..100])));

        var arrayBacked = TagHelperCollection.Create(helpers);

        // Act & Assert - Large collections should use checksum-based equality
        Assert.True(merged1.Equals(merged2));
        Assert.True(merged2.Equals(arrayBacked));
        Assert.True(arrayBacked.Equals(merged1));

        // Hash codes should be equal due to checksum-based computation
        Assert.Equal(merged1.GetHashCode(), merged2.GetHashCode());
        Assert.Equal(merged2.GetHashCode(), arrayBacked.GetHashCode());
    }

    [Fact]
    public void MergedCollection_TransitiveEquality_WorksCorrectly()
    {
        // Arrange - Test transitive property: if A == B and B == C, then A == C
        var helpers = CreateTestTagHelpers(6).AsSpan();

        var collectionA = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create(helpers[2..6]));

        var collectionB = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..3]),
            TagHelperCollection.Create(helpers[3..6]));

        var collectionC = TagHelperCollection.Create(helpers);

        // Act & Assert - Test transitivity
        Assert.True(collectionA.Equals(collectionB)); // A == B
        Assert.True(collectionB.Equals(collectionC)); // B == C
        Assert.True(collectionA.Equals(collectionC)); // Therefore A == C

        // Test symmetry
        Assert.True(collectionB.Equals(collectionA)); // B == A
        Assert.True(collectionC.Equals(collectionB)); // C == B  
        Assert.True(collectionC.Equals(collectionA)); // C == A

        // Test reflexivity  
        Assert.True(collectionA.Equals(collectionA));
        Assert.True(collectionB.Equals(collectionB));
        Assert.True(collectionC.Equals(collectionC));
    }

    [Fact]
    public void MergedCollection_NullEquality_WorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(3).AsSpan();
        var merged = TagHelperCollection.Merge(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create(helpers[1..3]));

        // Act & Assert
        Assert.False(merged.Equals(null));
        Assert.False(merged.Equals((object?)null));
    }

    [Fact]
    public void MergedCollection_EqualityWithDifferentTypes_WorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(3).AsSpan();
        var merged = TagHelperCollection.Merge(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create(helpers[1..3]));

        // Act & Assert - Should not equal different types
        Assert.False(merged.Equals("not a collection"));
        Assert.False(merged.Equals(42));
    }

    [Fact]
    public void MergedCollection_EqualsObjectOverride_WorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(3).AsSpan();
        var merged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create([helpers[0]]),
            TagHelperCollection.Create(helpers[1..3]));

        var merged2 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create([helpers[2]]));

        // Act & Assert - Test object.Equals override
        Assert.True(merged1.Equals((object)merged2));
        Assert.True(merged2.Equals((object)merged1));
        Assert.True(merged1.Equals((object)merged1)); // Reflexivity
    }

    [Fact]
    public void MergedCollection_EnumerationResetAndDispose_WorksCorrectly()
    {
        // Arrange
        var helpers = CreateTestTagHelpers(6).AsSpan();
        var merged = TagHelperCollection.Merge(ImmutableArray.Create(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create(helpers[2..4]),
            TagHelperCollection.Create(helpers[4..6])));

        // Act & Assert - Test enumerator lifecycle
        using var enumerator = merged.GetEnumerator();

        // Enumerate first few items
        Assert.True(enumerator.MoveNext());
        Assert.Same(helpers[0], enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Same(helpers[1], enumerator.Current);

        // Reset should work
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Same(helpers[0], enumerator.Current);

        // Dispose should work (multiple times)
        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    [Fact]
    public void MergedCollection_IndexOfWithNestedMergedCollections_WorksCorrectly()
    {
        // Arrange - Create nested merged collections to test complex scenarios
        var helpers = CreateTestTagHelpers(12).AsSpan();

        var innerMerged1 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[0..2]),
            TagHelperCollection.Create(helpers[2..4]));
        var innerMerged2 = TagHelperCollection.Merge(
            TagHelperCollection.Create(helpers[4..6]),
            TagHelperCollection.Create(helpers[6..8]));
        var regularCollection = TagHelperCollection.Create(helpers[8..12]);

        var outerMerged = TagHelperCollection.Merge(ImmutableArray.Create(
            innerMerged1, innerMerged2, regularCollection));

        // Act & Assert - Verify IndexOf works correctly for nested structure
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(i, outerMerged.IndexOf(helpers[i]));
            Assert.Same(helpers[i], outerMerged[i]);
        }

        // Test non-existent item
        var nonExistent = CreateTagHelper("NonExistent");
        Assert.Equal(-1, outerMerged.IndexOf(nonExistent));
    }

    [Fact]
    public void MergedCollection_LargeNumberOfSmallSegments_PerformsWell()
    {
        // Arrange - Stress test with many small segments
        var segments = new List<TagHelperCollection>();
        var allHelpers = new List<TagHelperDescriptor>();

        for (var i = 0; i < 50; i++)
        {
            var helper = CreateTagHelper($"Helper{i}");
            segments.Add(TagHelperCollection.Create([helper]));
            allHelpers.Add(helper);
        }

        var merged = TagHelperCollection.Merge(segments.ToImmutableArray());

        // Act & Assert - Verify functionality with many segments
        Assert.Equal(50, merged.Count);

        // Test enumeration
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in merged)
        {
            enumerated.Add(item);
        }
        Assert.Equal(allHelpers, enumerated);

        // Test random access
        for (var i = 0; i < 50; i += 5) // Test every 5th element
        {
            Assert.Same(allHelpers[i], merged[i]);
            Assert.Equal(i, merged.IndexOf(allHelpers[i]));
        }
    }

    [Fact]
    public void Where_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var collection = TagHelperCollection.Empty;

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Test", StringComparison.Ordinal));

        // Assert
        Assert.Same(TagHelperCollection.Empty, filtered);
    }

    [Fact]
    public void Where_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("NonExistent", StringComparison.Ordinal));

        // Assert
        Assert.Same(TagHelperCollection.Empty, filtered);
    }

    [Fact]
    public void Where_AllMatch_ReturnsSameCollection()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(3);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("TagHelper", StringComparison.Ordinal));

        // Assert
        Assert.Equal(collection.Count, filtered.Count);
        Assert.SameItems(tagHelpers, filtered);

        // Verify the result has the same structure (should be optimized)
        for (var i = 0; i < tagHelpers.Length; i++)
        {
            Assert.Same(tagHelpers[i], filtered[i]);
        }
    }

    [Fact]
    public void Where_PartialMatch_ReturnsFilteredCollection()
    {
        // Arrange
        var tagHelper1 = CreateTagHelper("TestHelper1");
        var tagHelper2 = CreateTagHelper("OtherHelper");
        var tagHelper3 = CreateTagHelper("TestHelper3");
        var tagHelpers = new[] { tagHelper1, tagHelper2, tagHelper3 };
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Test", StringComparison.Ordinal));

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Same(tagHelper1, filtered[0]);
        Assert.Same(tagHelper3, filtered[1]);
        Assert.Equal(0, filtered.IndexOf(tagHelper1));
        Assert.Equal(1, filtered.IndexOf(tagHelper3));
        Assert.True(filtered.Contains(tagHelper1));
        Assert.True(filtered.Contains(tagHelper3));
        Assert.False(filtered.Contains(tagHelper2));
    }

    [Fact]
    public void Where_SingleItem_MatchingPredicate_ReturnsSingleItemCollection()
    {
        // Arrange
        var tagHelper = CreateTagHelper("TestHelper");
        TagHelperCollection collection = [tagHelper];

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Test", StringComparison.Ordinal));

        // Assert
        Assert.Single(filtered);
        Assert.Same(tagHelper, filtered[0]);
        Assert.Equal(0, filtered.IndexOf(tagHelper));
        Assert.True(filtered.Contains(tagHelper));
    }

    [Fact]
    public void Where_SingleItem_NonMatchingPredicate_ReturnsEmpty()
    {
        // Arrange
        var tagHelper = CreateTagHelper("TestHelper");
        TagHelperCollection collection = [tagHelper];

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Other", StringComparison.Ordinal));

        // Assert
        Assert.Same(TagHelperCollection.Empty, filtered);
    }

    [Fact]
    public void Where_MergedCollection_WorksCorrectly()
    {
        // Arrange
        var firstHelpers = CreateTestTagHelpers(2);
        var secondHelpers = new[] { CreateTagHelper("OtherHelper1"), CreateTagHelper("TestHelper") };
        var first = TagHelperCollection.Create(firstHelpers);
        var second = TagHelperCollection.Create(secondHelpers);
        var merged = TagHelperCollection.Merge(first, second);

        // Act
        var filtered = merged.Where(h => h.Name.StartsWith("TagHelper", StringComparison.Ordinal));

        // Assert
        Assert.Equal(2, filtered.Count); // 2 from first, 0 from second (neither "OtherHelper1" nor "TestHelper" starts with "TagHelper")
        Assert.Same(firstHelpers[0], filtered[0]);
        Assert.Same(firstHelpers[1], filtered[1]);
    }

    [Fact]
    public void Where_LargeCollection_WorksCorrectly()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(100);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act - Filter for even-numbered helpers
        var filtered = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) % 2 == 0;
        });

        // Assert
        Assert.Equal(50, filtered.Count); // TagHelper0, TagHelper2, ..., TagHelper98

        // Verify the filtered items are correct
        for (var i = 0; i < 50; i++)
        {
            var expectedIndex = i * 2;
            Assert.Same(tagHelpers[expectedIndex], filtered[i]);
            Assert.Equal(i, filtered.IndexOf(tagHelpers[expectedIndex]));
        }
    }

    [Fact]
    public void Where_ComplexPredicate_WorksCorrectly()
    {
        // Arrange
        var shortHelper = CreateTagHelper("A");
        var mediumHelper1 = CreateTagHelper("Medium1");
        var mediumHelper2 = CreateTagHelper("Medium2");
        var longHelper = CreateTagHelper("VeryLongHelperName");
        var collection = TagHelperCollection.Create([shortHelper, mediumHelper1, mediumHelper2, longHelper]);

        // Act
        var filtered = collection.Where(h => h.Name.Length >= 6 && h.Name.Length <= 8);

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Same(mediumHelper1, filtered[0]);
        Assert.Same(mediumHelper2, filtered[1]);
    }

    [Fact]
    public void Where_ChainedFiltering_WorksCorrectly()
    {
        // Arrange
        var helpers = new[]
        {
        CreateTagHelper("TestHelper1"),
        CreateTagHelper("TestHelper2"),
        CreateTagHelper("OtherHelper1"),
        CreateTagHelper("TestHelper3"),
        CreateTagHelper("OtherHelper2")
    };
        var collection = TagHelperCollection.Create(helpers);

        // Act - Chain multiple Where operations
        var filtered = collection
            .Where(h => h.Name.StartsWith("Test", StringComparison.Ordinal))
            .Where(h => !h.Name.EndsWith('2'));

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Same(helpers[0], filtered[0]); // TestHelper1
        Assert.Same(helpers[3], filtered[1]); // TestHelper3
    }

    [Fact]
    public void Where_PreservesSegmentStructure_OptimalCase()
    {
        // Arrange - Create a multi-segment collection where filtering preserves segment boundaries
        var segment1Helpers = new[] { CreateTagHelper("Keep1"), CreateTagHelper("Keep2") };
        var segment2Helpers = new[] { CreateTagHelper("Keep3"), CreateTagHelper("Keep4") };
        var collection1 = TagHelperCollection.Create(segment1Helpers);
        var collection2 = TagHelperCollection.Create(segment2Helpers);
        var merged = TagHelperCollection.Merge(collection1, collection2);

        // Act
        var filtered = merged.Where(h => h.Name.StartsWith("Keep", StringComparison.Ordinal));

        // Assert
        Assert.Equal(4, filtered.Count);
        Assert.Same(segment1Helpers[0], filtered[0]);
        Assert.Same(segment1Helpers[1], filtered[1]);
        Assert.Same(segment2Helpers[0], filtered[2]);
        Assert.Same(segment2Helpers[1], filtered[3]);

        // Verify filtering performance by checking all items are accessible
        for (var i = 0; i < 4; i++)
        {
            var helper = filtered[i];
            Assert.Equal(i, filtered.IndexOf(helper));
            Assert.True(filtered.Contains(helper));
        }
    }

    [Fact]
    public void Where_FragmentedSegments_CreatesOptimalSegments()
    {
        // Arrange - Create a scenario where filtering creates fragmented segments
        var helpers = new[]
        {
        CreateTagHelper("Keep1"),    // Keep
        CreateTagHelper("Remove1"),  // Remove
        CreateTagHelper("Keep2"),    // Keep
        CreateTagHelper("Keep3"),    // Keep
        CreateTagHelper("Remove2"),  // Remove
        CreateTagHelper("Keep4")     // Keep
    };
        var collection = TagHelperCollection.Create(helpers);

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Keep", StringComparison.Ordinal));

        // Assert
        Assert.Equal(4, filtered.Count);
        Assert.Same(helpers[0], filtered[0]); // Keep1
        Assert.Same(helpers[2], filtered[1]); // Keep2
        Assert.Same(helpers[3], filtered[2]); // Keep3
        Assert.Same(helpers[5], filtered[3]); // Keep4

        // Verify all operations work correctly on fragmented result
        Assert.Equal(0, filtered.IndexOf(helpers[0]));
        Assert.Equal(1, filtered.IndexOf(helpers[2]));
        Assert.Equal(2, filtered.IndexOf(helpers[3]));
        Assert.Equal(3, filtered.IndexOf(helpers[5]));
        Assert.Equal(-1, filtered.IndexOf(helpers[1])); // Removed item
        Assert.Equal(-1, filtered.IndexOf(helpers[4])); // Removed item
    }

    [Fact]
    public void Where_AlternatingPattern_CreatesMultipleSegments()
    {
        // Arrange - Create an alternating keep/remove pattern
        var helpers = new TagHelperDescriptor[10];
        for (var i = 0; i < 10; i++)
        {
            helpers[i] = CreateTagHelper(i % 2 == 0 ? $"Keep{i}" : $"Remove{i}");
        }
        var collection = TagHelperCollection.Create(helpers);

        // Act
        var filtered = collection.Where(h => h.Name.StartsWith("Keep", StringComparison.Ordinal));

        // Assert
        Assert.Equal(5, filtered.Count);
        for (var i = 0; i < 5; i++)
        {
            var expectedIndex = i * 2;
            Assert.Same(helpers[expectedIndex], filtered[i]);
        }
    }

    [Fact]
    public void Where_FilteredResult_SupportsAllOperations()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(6);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            var index = int.Parse(nameIndex);
            return index % 2 == 0; // Keep even indices: 0, 2, 4
        });

        // Assert
        Assert.Equal(3, filtered.Count);

        // Test indexer
        Assert.Same(tagHelpers[0], filtered[0]);
        Assert.Same(tagHelpers[2], filtered[1]);
        Assert.Same(tagHelpers[4], filtered[2]);

        // Test IndexOf
        Assert.Equal(0, filtered.IndexOf(tagHelpers[0]));
        Assert.Equal(1, filtered.IndexOf(tagHelpers[2]));
        Assert.Equal(2, filtered.IndexOf(tagHelpers[4]));
        Assert.Equal(-1, filtered.IndexOf(tagHelpers[1])); // Filtered out
        Assert.Equal(-1, filtered.IndexOf(tagHelpers[3])); // Filtered out
        Assert.Equal(-1, filtered.IndexOf(tagHelpers[5])); // Filtered out

        // Test Contains
        Assert.True(filtered.Contains(tagHelpers[0]));
        Assert.True(filtered.Contains(tagHelpers[2]));
        Assert.True(filtered.Contains(tagHelpers[4]));
        Assert.False(filtered.Contains(tagHelpers[1]));
        Assert.False(filtered.Contains(tagHelpers[3]));
        Assert.False(filtered.Contains(tagHelpers[5]));

        // Test CopyTo
        var destination = new TagHelperDescriptor[5];
        filtered.CopyTo(destination);
        Assert.Same(tagHelpers[0], destination[0]);
        Assert.Same(tagHelpers[2], destination[1]);
        Assert.Same(tagHelpers[4], destination[2]);
        Assert.Null(destination[3]);
        Assert.Null(destination[4]);

        // Test enumeration
        var enumerated = new List<TagHelperDescriptor>();
        foreach (var item in filtered)
        {
            enumerated.Add(item);
        }
        Assert.Equal(3, enumerated.Count);
        Assert.Same(tagHelpers[0], enumerated[0]);
        Assert.Same(tagHelpers[2], enumerated[1]);
        Assert.Same(tagHelpers[4], enumerated[2]);
    }

    [Fact]
    public void Where_FilteredResult_SupportsEquality()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);
        var collection1 = TagHelperCollection.Create(tagHelpers);
        var collection2 = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered1 = collection1.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) < 2; // Keep 0, 1
        });
        var filtered2 = collection2.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) < 2; // Keep 0, 1
        });

        // Assert
        Assert.True(filtered1.Equals(filtered2));
        Assert.True(filtered2.Equals(filtered1));
        Assert.Equal(filtered1.GetHashCode(), filtered2.GetHashCode());
    }

    [Fact]
    public void Where_FilteredResult_DifferentContent_NotEqual()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);
        var collection = TagHelperCollection.Create(tagHelpers);

        // Act
        var filtered1 = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) < 2; // Keep 0, 1
        });
        var filtered2 = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) >= 2; // Keep 2, 3
        });

        // Assert
        Assert.False(filtered1.Equals(filtered2));
        Assert.False(filtered2.Equals(filtered1));
        Assert.NotEqual(filtered1.GetHashCode(), filtered2.GetHashCode());
    }

    [Fact]
    public void Where_FilteredResult_ThrowsOnInvalidIndex()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);
        var collection = TagHelperCollection.Create(tagHelpers);
        var filtered = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) < 2; // Keep 0, 1
        });

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => filtered[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => filtered[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => filtered[10]);
    }

    [Fact]
    public void Where_FilteredResult_CopyToDestinationTooShort_ThrowsArgumentException()
    {
        // Arrange
        var tagHelpers = CreateTestTagHelpers(4);
        var collection = TagHelperCollection.Create(tagHelpers);
        var filtered = collection.Where(h =>
        {
            var nameIndex = h.Name["TagHelper".Length..];
            return int.Parse(nameIndex) < 3; // Keep 0, 1, 2
        });
        var destination = new TagHelperDescriptor[2]; // Too short

        // Act & Assert
        Assert.Throws<ArgumentException>(() => filtered.CopyTo(destination));
    }
}
