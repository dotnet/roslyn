# The `TestAccessor` Pattern

The `TestAccessor` pattern allows production code to expose internal functionality for test purposes without making the internal functionality available to other production code. The pattern has two primary components:

1. A `TestAccessor` type, which contains the functionality available only for testing
2. A `GetTestAccessor()` method, which returns an instance of `TestAccessor`

The pattern relies on enforcement of a simple rule that no production code is allowed to call a `GetTestAccessor()` method. This is enforceable either through code reviews or through an analyzer. This pattern has many advantages over alternatives:

* The pattern does not require expanded accessibility (e.g. `internal` instead of `private`) for the purpose of testing
* The pattern is self-documenting: all properties and methods within a `TestAccessor` type are intended only for use in test code
* The pattern is consistent enough to enforce through static analysis (analyzers)
* The pattern is simple enough to enforce manually (code reviews)

## The `TestAccessor` Type

The `TestAccessor` type is typically defined as a nested structure. In the following example, the `SomeProductionType.TestAccessor.PrivateStateData` property allows test code to read and write the value of the private field `SomeProductionType._privateStateData` without exposing the `_privateStateData` field to other production code.

```csharp
internal class SomeProductionType
{
  private int _privateStateData;

  internal readonly struct TestAccessor
  {
    private readonly SomeProductionType _instance;

    internal TestAccessor(SomeProductionType instance)
    {
      _instance = instance;
    }

    internal ref int PrivateStateData => ref _instance._privateStateData;
  }
}
```

## The `GetTestAccessor()` Method

The `GetTestAccessor()` method is always defined as follows:

```csharp
internal TestAccessor GetTestAccessor()
  => new TestAccessor(this);
```
