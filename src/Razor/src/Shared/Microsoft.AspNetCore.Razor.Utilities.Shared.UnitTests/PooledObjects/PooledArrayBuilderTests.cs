// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;
using SR = Microsoft.AspNetCore.Razor.Utilities.Shared.Resources.SR;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.PooledObjects;

public class PooledArrayBuilderTests
{
    [Theory]
    [CombinatorialData]
    public void AddElements([CombinatorialRange(0, 8)] int count)
    {
        using var builder = new PooledArrayBuilder<int>();

        for (var i = 0; i < count; i++)
        {
            builder.Add(i);
        }

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i, builder[i]);
        }

        var result = builder.ToImmutableAndClear();

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i, result[i]);
        }
    }

    public static TheoryData<int, int> RemoveAtIndex_Data
    {
        get
        {
            var data = new TheoryData<int, int>();

            for (var count = 0; count < 8; count++)
            {
                for (var removeIndex = 0; removeIndex < 8; removeIndex++)
                {
                    if (removeIndex < count)
                    {
                        data.Add(count, removeIndex);
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RemoveAtIndex_Data))]
    public void RemoveAtIndex(int count, int removeIndex)
    {
        using var builder = new PooledArrayBuilder<int>();

        for (var i = 0; i < count; i++)
        {
            builder.Add(i);
        }

        var newCount = count;
        var newValue = removeIndex;

        // Now, remove each element at removeIndex.
        for (var i = removeIndex; i < builder.Count; i++)
        {
            builder.RemoveAt(removeIndex);
            newCount--;
            newValue++;

            Assert.Equal(newCount, builder.Count);

            // Check the values starting at removeIndex.
            for (var j = removeIndex; j < newCount; j++)
            {
                Assert.Equal(newValue + (j - removeIndex), builder[j]);
            }
        }
    }

    [Fact]
    public void UseToImmutableAndClearAndReuse()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        for (var i = 0; i < 10; i++)
        {
            builder.Add(i);
        }

        // Verify that the builder contains 10 items within its inner array builder.
        builder.Validate(static t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.Equal(10, t.Capacity);
            Assert.NotNull(t.InnerArrayBuilder);

            Assert.Equal(10, t.InnerArrayBuilder.Count);
            Assert.Equal(10, t.InnerArrayBuilder.Capacity);
        });

        var result = builder.ToImmutableAndClear();

        // After calling ToImmutableAndClear, the builder should contain 0 items in its inner array builder
        // and the capacity should have been set to 0.
        builder.Validate(static t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.Equal(10, t.Capacity);
            Assert.NotNull(t.InnerArrayBuilder);

            Assert.Empty(t.InnerArrayBuilder);
            Assert.Equal(0, t.InnerArrayBuilder.Capacity);
        });

        // Add another 10 items to the builder. At the end, the inner array builder
        // should hold 10 items with a capacity of 10.
        for (var i = 0; i < 10; i++)
        {
            builder.Add(i);
        }

        // Verify that the builder contains 10 items within its inner array builder.
        builder.Validate(static t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.Equal(10, t.Capacity);
            Assert.NotNull(t.InnerArrayBuilder);

            Assert.Equal(10, t.InnerArrayBuilder.Count);
            Assert.Equal(10, t.InnerArrayBuilder.Capacity);
        });
    }

    private static Func<int, bool> IsEven => x => x % 2 == 0;
    private static Func<int, bool> IsOdd => x => x % 2 != 0;

    [Fact]
    public void Any()
    {
        using var builder = new PooledArrayBuilder<int>();

        Assert.False(builder.Any());

        builder.Add(19);

        Assert.True(builder.Any());

        builder.Add(23);

        Assert.True(builder.Any(IsOdd));

        // ... but no even numbers
        Assert.False(builder.Any(IsEven));
    }

    [Fact]
    public void All()
    {
        using var builder = new PooledArrayBuilder<int>();

        Assert.True(builder.All(IsEven));

        builder.Add(19);

        Assert.False(builder.All(IsEven));

        builder.Add(23);

        Assert.True(builder.All(IsOdd));

        builder.Add(42);

        Assert.False(builder.All(IsOdd));
    }

    [Fact]
    public void FirstAndLast()
    {
        using var builder = new PooledArrayBuilder<int>();

        var exception1 = Assert.Throws<InvalidOperationException>(() => builder.First());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);

        Assert.Equal(default, builder.FirstOrDefault());

        var exception2 = Assert.Throws<InvalidOperationException>(() => builder.Last());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);

        Assert.Equal(default, builder.LastOrDefault());

        builder.Add(19);

        Assert.Equal(19, builder.First());
        Assert.Equal(19, builder.FirstOrDefault());
        Assert.Equal(19, builder.Last());
        Assert.Equal(19, builder.LastOrDefault());

        builder.Add(23);

        Assert.Equal(19, builder.First());
        Assert.Equal(19, builder.FirstOrDefault());
        Assert.Equal(23, builder.Last());
        Assert.Equal(23, builder.LastOrDefault());
    }

    [Fact]
    public void FirstAndLastWithPredicate()
    {
        using var builder = new PooledArrayBuilder<int>();

        var exception1 = Assert.Throws<InvalidOperationException>(() => builder.First(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception1.Message);

        Assert.Equal(default, builder.FirstOrDefault(IsOdd));

        var exception2 = Assert.Throws<InvalidOperationException>(() => builder.Last(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception2.Message);

        Assert.Equal(default, builder.LastOrDefault(IsOdd));

        builder.Add(19);

        Assert.Equal(19, builder.First(IsOdd));
        Assert.Equal(19, builder.FirstOrDefault(IsOdd));
        Assert.Equal(19, builder.Last(IsOdd));
        Assert.Equal(19, builder.LastOrDefault(IsOdd));

        builder.Add(23);

        Assert.Equal(19, builder.First(IsOdd));
        Assert.Equal(19, builder.FirstOrDefault(IsOdd));
        Assert.Equal(23, builder.Last(IsOdd));
        Assert.Equal(23, builder.LastOrDefault(IsOdd));

        var exception3 = Assert.Throws<InvalidOperationException>(() => builder.First(IsEven));
        Assert.Equal(SR.Contains_no_matching_elements, exception3.Message);

        Assert.Equal(default, builder.FirstOrDefault(IsEven));

        var exception4 = Assert.Throws<InvalidOperationException>(() => builder.Last(IsEven));
        Assert.Equal(SR.Contains_no_matching_elements, exception4.Message);

        Assert.Equal(default, builder.LastOrDefault(IsEven));

        builder.Add(42);

        Assert.Equal(42, builder.First(IsEven));
        Assert.Equal(42, builder.FirstOrDefault(IsEven));
        Assert.Equal(42, builder.Last(IsEven));
        Assert.Equal(42, builder.LastOrDefault(IsEven));
    }

    [Fact]
    public void Single()
    {
        using var builder = new PooledArrayBuilder<int>();

        var exception1 = Assert.Throws<InvalidOperationException>(() => builder.Single());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);
        Assert.Equal(default, builder.SingleOrDefault());

        builder.Add(19);

        Assert.Equal(19, builder.Single());
        Assert.Equal(19, builder.SingleOrDefault());

        builder.Add(23);

        var exception2 = Assert.Throws<InvalidOperationException>(() => builder.Single());
        Assert.Equal(SR.Contains_more_than_one_element, exception2.Message);
        var exception3 = Assert.Throws<InvalidOperationException>(() => builder.SingleOrDefault());
        Assert.Equal(SR.Contains_more_than_one_element, exception2.Message);
    }

    [Fact]
    public void SingleWithPredicate()
    {
        using var builder = new PooledArrayBuilder<int>();

        var exception1 = Assert.Throws<InvalidOperationException>(() => builder.Single(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception1.Message);
        Assert.Equal(default, builder.SingleOrDefault(IsOdd));

        builder.Add(19);

        Assert.Equal(19, builder.Single(IsOdd));
        Assert.Equal(19, builder.SingleOrDefault(IsOdd));

        builder.Add(23);

        var exception2 = Assert.Throws<InvalidOperationException>(() => builder.Single(IsOdd));
        Assert.Equal(SR.Contains_more_than_one_matching_element, exception2.Message);
        var exception3 = Assert.Throws<InvalidOperationException>(() => builder.SingleOrDefault(IsOdd));
        Assert.Equal(SR.Contains_more_than_one_matching_element, exception2.Message);

        builder.Add(42);

        Assert.Equal(42, builder.Single(IsEven));
        Assert.Equal(42, builder.SingleOrDefault(IsEven));
    }

    [Fact]
    public void Add_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        builder.Add(42);

        Assert.Equal(1, builder.Count);
        Assert.Equal(42, builder[0]);

        // Verify using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(1, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void Add_MultipleItemsWithinInlineCapacity()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add multiple items, but still within inline capacity (which is 4)
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Verify still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void Add_FillInlineCapacity()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add exactly InlineCapacity (which is 4) items
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);
        builder.Add(4);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);

        // Verify still using inline storage when exactly at capacity
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void Add_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add up to InlineCapacity
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);
        builder.Add(4);

        // Verify we're still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });

        // Add one more item to trigger transition to builder
        builder.Add(5);

        Assert.Equal(5, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
        Assert.Equal(5, builder[4]);

        // Verify we're now using a builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
            Assert.Equal(5, t.InnerArrayBuilder.Count);
        });
    }

    [Fact]
    public void Add_AlreadyUsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i);
        }

        // Verify builder is being used
        builder.Validate(t =>
        {
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Add another item
        builder.Add(42);

        Assert.Equal(6, builder.Count);
        Assert.Equal(42, builder[5]);

        // Verify still using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
            Assert.Equal(6, t.InnerArrayBuilder.Count);
        });
    }

    [Fact]
    public void Add_WithExplicitInitialCapacity()
    {
        // Create builder with initial capacity larger than inline capacity
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add item
        builder.Add(42);

        Assert.Equal(1, builder.Count);
        Assert.Equal(42, builder[0]);

        // Even with large capacity, should still use inline storage until needed
        builder.Validate(t =>
        {
            Assert.Equal(1, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
            Assert.Equal(10, t.Capacity);
        });

        // Add more items to exceed inline capacity
        for (var i = 0; i < 4; i++)
        {
            builder.Add(i);
        }

        // Verify builder is now being used and has correct capacity
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
            Assert.Equal(10, t.InnerArrayBuilder.Capacity);
        });
    }

    [Fact]
    public void Add_ReferenceType()
    {
        // Test with reference type
        using var builder = new PooledArrayBuilder<string>();

        builder.Add("first");
        builder.Add("second");

        Assert.Equal(2, builder.Count);
        Assert.Equal("first", builder[0]);
        Assert.Equal("second", builder[1]);
    }

    [Fact]
    public void Add_ManyItems()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add many items (more than inline capacity)
        var expectedItems = new List<int>();
        for (var i = 0; i < 100; i++)
        {
            builder.Add(i);
            expectedItems.Add(i);
        }

        Assert.Equal(100, builder.Count);

        // Verify all items are correctly stored
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(expectedItems[i], builder[i]);
        }

        // Verify using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_EmptyCollection()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Test with various empty collection types
        builder.AddRange(Array.Empty<int>());
        builder.AddRange(ImmutableArray<int>.Empty);
        builder.AddRange(Enumerable.Empty<int>());
        builder.AddRange(ReadOnlySpan<int>.Empty);

        // Verify no items were added
        Assert.Equal(0, builder.Count);
    }

    [Theory]
    [InlineData(1)] // Small collection that fits in inline storage
    [InlineData(4)] // Collection that exactly fills inline storage
    [InlineData(10)] // Collection that requires builder
    public void AddRange_Array(int count)
    {
        using var builder = new PooledArrayBuilder<int>();

        var items = Enumerable.Range(1, count).ToArray();
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(count, builder.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }

        // Verify correct storage is used
        builder.Validate(t =>
        {
            if (count <= 4)
            {
                Assert.Equal(count, t.InlineItemCount);
                Assert.Null(t.InnerArrayBuilder);
            }
            else
            {
                Assert.Equal(0, t.InlineItemCount);
                Assert.NotNull(t.InnerArrayBuilder);
            }
        });
    }

    [Fact]
    public void AddRange_ImmutableArray()
    {
        using var builder = new PooledArrayBuilder<int>();

        var items = ImmutableArray.Create(1, 2, 3);
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Verify inline storage is used (3 items < InlineCapacity)
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_ReadOnlySpan_InlineStorage()
    {
        using var builder = new PooledArrayBuilder<int>();

        ReadOnlySpan<int> items = [1, 2, 3];
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Verify inline storage is used
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_ReadOnlySpan_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // First add 2 items to inline storage
        builder.Add(1);
        builder.Add(2);

        // Then add 3 more via span (exceeding InlineCapacity of 4)
        ReadOnlySpan<int> items = [3, 4, 5];
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(5, builder.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }

        // Verify builder is now used
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_IEnumerable_WithCount()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Use a list which has a known count
        var items = new List<int> { 1, 2, 3 };
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Verify inline storage is used
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_IEnumerable_WithoutCount()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Use a generator that doesn't have a count until enumerated
        var items = GetItems();
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Verify inline storage is used
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });

        // Local iterator method that doesn't have a known count
        static IEnumerable<int> GetItems()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    [Fact]
    public void AddRange_IEnumerable_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // First add 2 items to inline storage
        builder.Add(1);
        builder.Add(2);

        // Then add 4 more via enumerable (exceeding InlineCapacity of 4)
        var items = new List<int> { 3, 4, 5, 6 };
        builder.AddRange(items);

        // Verify items were added correctly
        Assert.Equal(6, builder.Count);
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }

        // Verify builder is now used
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void AddRange_AlreadyUsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i);
        }

        // Verify builder is being used
        builder.Validate(t =>
        {
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Add items via AddRange
        var items = new List<int> { 5, 6, 7, 8, 9 };
        builder.AddRange(items);

        // Verify all items were added correctly
        Assert.Equal(10, builder.Count);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i, builder[i]);
        }
    }

    [Fact]
    public void AddRange_WithExplicitCapacity()
    {
        // Create builder with initial capacity larger than inline capacity
        using var builder = new PooledArrayBuilder<int>(capacity: 10);

        // Add items via AddRange
        var array = new[] { 1, 2, 3, 4, 5 };
        builder.AddRange(array);

        // Verify items were added correctly
        Assert.Equal(5, builder.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }

        // Because we've exceeded inline capacity, the builder should be created with the specified capacity
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
            Assert.Equal(10, t.Capacity);
        });
    }

    [Fact]
    public void AddRange_Repeated()
    {
        using var builder = new PooledArrayBuilder<int>();

        // First add some items
        var array1 = new[] { 1, 2 };
        builder.AddRange(array1);

        // Add more items
        var array2 = new[] { 3, 4 };
        builder.AddRange(array2);

        // Verify all items were added correctly
        Assert.Equal(4, builder.Count);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }
    }

    [Fact]
    public void AddRange_WithGenericStruct()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Create a struct that implements IReadOnlyList<int>
        var array = new[] { 1, 2, 3 };
        var list = new StructList<int>(array);

        // Add items from struct
        builder.AddRange(list);

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void AddRange_WithGenericStruct_WithIndex()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Create a struct that implements IReadOnlyList<int>
        var array = new[] { 0, 1, 2, 3, 4 };
        var list = new StructList<int>(array);

        // Add items from struct with index/count
        builder.AddRange(list, 1, 3); // Should add values 1, 2, 3

        // Verify items were added correctly
        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void Insert_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        builder.Insert(0, 42);

        Assert.Equal(1, builder.Count);
        Assert.Equal(42, builder[0]);
    }

    [Fact]
    public void Insert_AtBeginning()
    {
        using var builder = new PooledArrayBuilder<int>();

        builder.Add(1);
        builder.Add(2);
        builder.Insert(0, 42);

        Assert.Equal(3, builder.Count);
        Assert.Equal(42, builder[0]);
        Assert.Equal(1, builder[1]);
        Assert.Equal(2, builder[2]);
    }

    [Fact]
    public void Insert_AtMiddle()
    {
        using var builder = new PooledArrayBuilder<int>();

        builder.Add(1);
        builder.Add(2);
        builder.Insert(1, 42);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(42, builder[1]);
        Assert.Equal(2, builder[2]);
    }

    [Fact]
    public void Insert_AtEnd()
    {
        using var builder = new PooledArrayBuilder<int>();

        builder.Add(1);
        builder.Add(2);
        builder.Insert(2, 42);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(42, builder[2]);
    }

    [Fact]
    public void Insert_TransitionFromInlineToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add up to InlineCapacity (which is 4)
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);
        builder.Add(4);

        // Verify we have exactly 4 items
        Assert.Equal(4, builder.Count);

        // Insert a 5th item, which should trigger conversion to builder
        builder.Insert(2, 42);

        Assert.Equal(5, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(42, builder[2]);
        Assert.Equal(3, builder[3]);
        Assert.Equal(4, builder[4]);

        // Verify the builder is now being used
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Insert_WithInlineStorage(int insertIndex)
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add 3 items (less than InlineCapacity)
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);

        // Insert at the specified index
        builder.Insert(insertIndex, 42);

        // Verify the item was inserted correctly
        Assert.Equal(4, builder.Count);

        for (var i = 0; i < 4; i++)
        {
            if (i < insertIndex)
            {
                Assert.Equal(i + 1, builder[i]);
            }
            else if (i == insertIndex)
            {
                Assert.Equal(42, builder[i]);
            }
            else
            {
                Assert.Equal(i, builder[i]);
            }
        }

        // Verify we're still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void Insert_UsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i);
        }

        // Verify builder is being used
        builder.Validate(t =>
        {
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Insert in the middle
        builder.Insert(2, 42);

        Assert.Equal(6, builder.Count);
        Assert.Equal(0, builder[0]);
        Assert.Equal(1, builder[1]);
        Assert.Equal(42, builder[2]);
        Assert.Equal(2, builder[3]);
        Assert.Equal(3, builder[4]);
        Assert.Equal(4, builder[5]);
    }

    [Fact]
    public void InsertRange_EmptyCollection()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        // Insert empty array
        builder.InsertRange(1, Array.Empty<int>());

        Assert.Equal(2, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);

        // Insert empty immutable array
        builder.InsertRange(1, ImmutableArray<int>.Empty);

        Assert.Equal(2, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
    }

    [Fact]
    public void InsertRange_AtBeginning()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(3);
        builder.Add(4);

        var array = new[] { 1, 2 };
        builder.InsertRange(0, array);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_AtMiddle()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(4);

        var array = new[] { 2, 3 };
        builder.InsertRange(1, array);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_AtEnd()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        var array = new[] { 3, 4 };
        builder.InsertRange(2, array);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_ImmutableArray()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(4);

        var items = ImmutableArray.Create(2, 3);
        builder.InsertRange(1, items);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_WithInlineStorage()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add 2 items (less than InlineCapacity)
        builder.Add(1);
        builder.Add(4);

        // Insert 2 more items (total still within InlineCapacity)
        var array = new[] { 2, 3 };
        builder.InsertRange(1, array);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);

        // Verify still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_TransitionFromInlineToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add items up to InlineCapacity (which is 4)
        builder.Add(1);
        builder.Add(3);
        builder.Add(4);
        builder.Add(5);

        // Insert items that cause transition to builder
        var array = new[] { 2, 2 };
        builder.InsertRange(1, array);

        Assert.Equal(6, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(2, builder[2]);
        Assert.Equal(3, builder[3]);
        Assert.Equal(4, builder[4]);
        Assert.Equal(5, builder[5]);

        // Verify builder is now being used
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_UsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i * 2);
        }

        // Verify builder is being used
        builder.Validate(t =>
        {
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Insert in the middle
        var array = new[] { 3, 5 };
        builder.InsertRange(2, array);

        Assert.Equal(7, builder.Count);
        Assert.Equal(0, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(5, builder[3]);
        Assert.Equal(4, builder[4]);
        Assert.Equal(6, builder[5]);
        Assert.Equal(8, builder[6]);
    }

    [Fact]
    public void InsertRange_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        var array = new[] { 1, 2, 3 };
        builder.InsertRange(0, array);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void InsertRange_EmptyBuilder_ImmutableArray()
    {
        using var builder = new PooledArrayBuilder<int>();

        var items = ImmutableArray.Create(1, 2, 3);
        builder.InsertRange(0, items);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void InsertRange_LargeCollection()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Create a large collection that exceeds inline capacity
        builder.InsertRange(0, Enumerable.Range(1, 100));

        Assert.Equal(100, builder.Count);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(i + 1, builder[i]);
        }

        // Verify builder is being used
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_EmptySpan()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(3);

        // Insert empty span
        ReadOnlySpan<int> emptySpan = [];
        builder.InsertRange(1, emptySpan);

        // Should not change the builder
        Assert.Equal(2, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(3, builder[1]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_AtBeginning()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(3);
        builder.Add(4);

        // Insert at beginning
        ReadOnlySpan<int> itemsToInsert = [1, 2];
        builder.InsertRange(0, itemsToInsert);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_AtMiddle()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(4);

        // Insert in middle
        ReadOnlySpan<int> itemsToInsert = [2, 3];
        builder.InsertRange(1, itemsToInsert);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_AtEnd()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        // Insert at end
        ReadOnlySpan<int> itemsToInsert = [3, 4];
        builder.InsertRange(2, itemsToInsert);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_SingleItem()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(3);

        // Insert single item
        ReadOnlySpan<int> itemToInsert = [2];
        builder.InsertRange(1, itemToInsert);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_WithinInlineCapacity()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add 2 items (less than inline capacity)
        builder.Add(1);
        builder.Add(4);

        // Insert 2 more (still within inline capacity)
        ReadOnlySpan<int> itemsToInsert = [2, 3];
        builder.InsertRange(1, itemsToInsert);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);

        // Verify still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add items to reach inline capacity (which is 4)
        builder.Add(1);
        builder.Add(4);
        builder.Add(5);
        builder.Add(6);

        // Verify using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });

        // Insert items that will cause transition to builder
        ReadOnlySpan<int> itemsToInsert = [2, 3];
        builder.InsertRange(1, itemsToInsert);

        Assert.Equal(6, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
        Assert.Equal(5, builder[4]);
        Assert.Equal(6, builder[5]);

        // Verify now using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_AlreadyUsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i * 2);
        }

        // Verify already using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Insert into builder
        ReadOnlySpan<int> itemsToInsert = [3, 5];
        builder.InsertRange(2, itemsToInsert);

        Assert.Equal(7, builder.Count);
        Assert.Equal(0, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(5, builder[3]);
        Assert.Equal(4, builder[4]);
        Assert.Equal(6, builder[5]);
        Assert.Equal(8, builder[6]);
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        ReadOnlySpan<int> itemsToInsert = [1, 2, 3];
        builder.InsertRange(0, itemsToInsert);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Should use inline storage
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_ReadOnlySpan_LargeSpan()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(0);

        // Create array with more elements than inline capacity
        var largeArray = new int[10];
        for (var i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = i + 1;
        }

        ReadOnlySpan<int> largeSpan = largeArray;
        builder.InsertRange(1, largeSpan);

        Assert.Equal(11, builder.Count);
        Assert.Equal(0, builder[0]);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i + 1, builder[i + 1]);
        }

        // Should be using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(4);

        // Create a struct that implements IReadOnlyList<int>
        var array = new[] { 2, 3 };
        var list = new StructList<int>(array);

        // Use the simpler overload that inserts the entire list
        builder.InsertRange(1, list);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_AtBeginning()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(3);
        builder.Add(4);

        var array = new[] { 1, 2 };
        var list = new StructList<int>(array);

        // Insert at beginning
        builder.InsertRange(0, list);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_AtEnd()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        var array = new[] { 3, 4 };
        var list = new StructList<int>(array);

        // Insert at end
        builder.InsertRange(2, list);

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_EmptyList()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        // Create empty struct list
        var array = Array.Empty<int>();
        var list = new StructList<int>(array);

        // Insert empty list
        builder.InsertRange(1, list);

        // Should not change the builder
        Assert.Equal(2, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add items to reach inline capacity (which is 4)
        builder.Add(1);
        builder.Add(4);
        builder.Add(5);
        builder.Add(6);

        // Verify using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });

        // Insert items that will cause transition to builder
        var array = new[] { 2, 3 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list);

        Assert.Equal(6, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
        Assert.Equal(5, builder[4]);
        Assert.Equal(6, builder[5]);

        // Verify now using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        var array = new[] { 1, 2, 3 };
        var list = new StructList<int>(array);

        // Insert into empty builder
        builder.InsertRange(0, list);

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void InsertRange_StructList_SimpleOverload_LargeCollection()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(0);

        // Create a large array with more elements than inline capacity
        var largeArray = new int[10];
        for (var i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = i + 1;
        }

        var list = new StructList<int>(largeArray);
        builder.InsertRange(1, list);

        Assert.Equal(11, builder.Count);
        Assert.Equal(0, builder[0]);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i + 1, builder[i + 1]);
        }

        // Should be using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_EmptyCount()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        // Create a list with items
        var array = new[] { 10, 20, 30 };
        var list = new StructList<int>(array);

        // Insert with count = 0
        builder.InsertRange(1, list, 0, 0);

        // Should not change the builder
        Assert.Equal(2, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
    }

    [Fact]
    public void InsertRange_StructList_AtBeginning()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(3);
        builder.Add(4);

        // Insert at beginning
        var array = new[] { 1, 2, 3 };
        var list = new StructList<int>(array);
        builder.InsertRange(0, list, 0, 2); // Insert first 2 items

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_AtMiddle()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(4);

        // Insert in middle
        var array = new[] { 10, 2, 3, 20 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list, 1, 2); // Insert items at index 1,2 (values 2,3)

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_AtEnd()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(2);

        // Insert at end
        var array = new[] { 0, 3, 4, 5 };
        var list = new StructList<int>(array);
        builder.InsertRange(2, list, 1, 2); // Insert items at index 1,2 (values 3,4)

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
    }

    [Fact]
    public void InsertRange_StructList_SingleItem()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(3);

        // Insert single item
        var array = new[] { 0, 2, 4 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list, 1, 1); // Insert item at index 1 (value 2)

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
    }

    [Fact]
    public void InsertRange_StructList_WithinInlineCapacity()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add 2 items (less than inline capacity)
        builder.Add(1);
        builder.Add(4);

        // Insert 2 more (still within inline capacity)
        var array = new[] { 0, 2, 3, 5 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list, 1, 2); // Insert items at index 1,2 (values 2,3)

        Assert.Equal(4, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);

        // Verify still using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_TransitionToBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        // Add items to reach inline capacity (which is 4)
        builder.Add(1);
        builder.Add(4);
        builder.Add(5);
        builder.Add(6);

        // Verify using inline storage
        builder.Validate(t =>
        {
            Assert.Equal(4, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });

        // Insert items that will cause transition to builder
        var array = new[] { 0, 2, 3, 7 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list, 1, 2); // Insert items at index 1,2 (values 2,3)

        Assert.Equal(6, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
        Assert.Equal(5, builder[4]);
        Assert.Equal(6, builder[5]);

        // Verify now using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_AlreadyUsingBuilder()
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: 10, builderPool);

        // Add enough items to ensure builder is used
        for (var i = 0; i < 5; i++)
        {
            builder.Add(i * 2);
        }

        // Verify already using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });

        // Insert into builder
        var array = new[] { 0, 3, 5, 7 };
        var list = new StructList<int>(array);
        builder.InsertRange(2, list, 1, 2); // Insert items at index 1,2 (values 3,5)

        Assert.Equal(7, builder.Count);
        Assert.Equal(0, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(5, builder[3]);
        Assert.Equal(4, builder[4]);
        Assert.Equal(6, builder[5]);
        Assert.Equal(8, builder[6]);
    }

    [Fact]
    public void InsertRange_StructList_EmptyBuilder()
    {
        using var builder = new PooledArrayBuilder<int>();

        var array = new[] { 1, 2, 3, 4 };
        var list = new StructList<int>(array);
        builder.InsertRange(0, list, 0, 3); // Insert first 3 items

        Assert.Equal(3, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);

        // Should use inline storage
        builder.Validate(t =>
        {
            Assert.Equal(3, t.InlineItemCount);
            Assert.Null(t.InnerArrayBuilder);
        });
    }

    [Fact]
    public void InsertRange_StructList_PartialRange()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(1);
        builder.Add(5);

        // Insert from middle of source
        var array = new[] { 10, 20, 2, 3, 4, 30, 40 };
        var list = new StructList<int>(array);
        builder.InsertRange(1, list, 2, 3); // Insert 3 items starting at index 2 (values 2,3,4)

        Assert.Equal(5, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
        Assert.Equal(3, builder[2]);
        Assert.Equal(4, builder[3]);
        Assert.Equal(5, builder[4]);
    }

    [Fact]
    public void InsertRange_StructList_LargeCollection()
    {
        using var builder = new PooledArrayBuilder<int>();
        builder.Add(0);

        // Create a large array with more elements than inline capacity
        var largeArray = new int[12];
        for (var i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = i + 1;
        }

        var list = new StructList<int>(largeArray);
        builder.InsertRange(1, list, 0, 10);

        Assert.Equal(11, builder.Count);
        Assert.Equal(0, builder[0]);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i + 1, builder[i + 1]);
        }

        // Should be using builder
        builder.Validate(t =>
        {
            Assert.Equal(0, t.InlineItemCount);
            Assert.NotNull(t.InnerArrayBuilder);
        });
    }

    // Helper struct that implements IReadOnlyList<T>
    private readonly struct StructList<T>(T[] items) : IReadOnlyList<T>
    {
        public T this[int index] => items[index];
        public int Count => items.Length;
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
}
