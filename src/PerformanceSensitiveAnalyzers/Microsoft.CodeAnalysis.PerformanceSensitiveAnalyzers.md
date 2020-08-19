# Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers

## HAA0101: Array allocation for params parameter

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

This call site is calling into a function with a 'params' parameter. This results in an array allocation.

## HAA0102: Non-overridden virtual method call on value type

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Non-overridden virtual method call on a value type adds a boxing or constrained instruction

## [HAA0201](http://msdn.microsoft.com/en-us/library/2839d5h5(v=vs.110).aspx): Implicit string concatenation allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Considering using StringBuilder

## [HAA0202](http://msdn.microsoft.com/en-us/library/yz2be5wk.aspx): Value type to reference type conversion allocation for string concatenation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Value type ({0}) is being boxed to a reference type for a string concatenation

## HAA0301: Closure Allocation Source

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Heap allocation of closure Captures: {0}

## HAA0302: Display class allocation to capture closure

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

The compiler will emit a class that will hold this as a field to allow capturing of this closure

## HAA0303: Lambda or anonymous method in a generic method allocates a delegate instance

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Considering moving this out of the generic method

## HAA0401: Possible allocation of reference type enumerator

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Non-ValueType enumerator may result in a heap allocation

## HAA0501: Explicit new array type allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|

### Rule description

Explicit new array type allocation

## HAA0502: Explicit new reference type allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|

### Rule description

Explicit new reference type allocation

## [HAA0503](http://msdn.microsoft.com/en-us/library/bb397696.aspx): Explicit new anonymous object allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|

### Rule description

Explicit new anonymous object allocation

## HAA0506: Let clause induced allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|

### Rule description

Let clause induced allocation

## HAA0601: Value type to reference type conversion causing boxing allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable.

## HAA0602: Delegate on struct instance caused a boxing allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Struct instance method being used for delegate creation, this will result in a boxing instruction

## HAA0603: Delegate allocation from a method group

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

This will allocate a delegate instance

## HAA0604: Delegate allocation from a method group

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|

### Rule description

This will allocate a delegate instance

