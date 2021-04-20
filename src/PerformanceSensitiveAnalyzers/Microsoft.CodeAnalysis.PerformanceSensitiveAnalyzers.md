# Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers

## HAA0101: Array allocation for params parameter

This call site is calling into a function with a 'params' parameter. This results in an array allocation.

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0102: Non-overridden virtual method call on value type

Non-overridden virtual method call on a value type adds a boxing or constrained instruction

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [HAA0201](http://msdn.microsoft.com/en-us/library/2839d5h5(v=vs.110).aspx): Implicit string concatenation allocation

Considering using StringBuilder

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [HAA0202](http://msdn.microsoft.com/en-us/library/yz2be5wk.aspx): Value type to reference type conversion allocation for string concatenation

Value type ({0}) is being boxed to a reference type for a string concatenation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0301: Closure Allocation Source

Heap allocation of closure Captures: {0}

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0302: Display class allocation to capture closure

The compiler will emit a class that will hold this as a field to allow capturing of this closure

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0303: Lambda or anonymous method in a generic method allocates a delegate instance

Considering moving this out of the generic method

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0401: Possible allocation of reference type enumerator

Non-ValueType enumerator may result in a heap allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0501: Explicit new array type allocation

Explicit new array type allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|True|
---

## HAA0502: Explicit new reference type allocation

Explicit new reference type allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|True|
---

## [HAA0503](http://msdn.microsoft.com/en-us/library/bb397696.aspx): Explicit new anonymous object allocation

Explicit new anonymous object allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|
---

## HAA0506: Let clause induced allocation

Let clause induced allocation

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|
---

## HAA0601: Value type to reference type conversion causing boxing allocation

Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable.

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0602: Delegate on struct instance caused a boxing allocation

Struct instance method being used for delegate creation, this will result in a boxing instruction

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0603: Delegate allocation from a method group

This will allocate a delegate instance

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## HAA0604: Delegate allocation from a method group

This will allocate a delegate instance

|Item|Value|
|-|-|
|Category|Performance|
|Enabled|True|
|Severity|Info|
|CodeFix|False|
---
