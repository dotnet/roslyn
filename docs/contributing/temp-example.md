# Wrapping indentation configuration

Here's a few examples:

## Scenario 1: Conditionals

### Before: 

```csharp 
if (conditional1 (&& conditional2 && (conditional3 || conditional4)) || conditional5) 
    DoThing();
```

### After (with wrapping enabled)

```csharp
if (conditional1
    && (conditional2
    && (conditional3
    || conditional4))
    || conditional5)
    DoSomething(); 
```

### After (with wrapping and indentation enabled)

```csharp
if (conditional1
        && (conditional2
            && (conditional3
                    || conditional4))
        || conditional5)
    DoSomething();
```

## Scenario 2: Call chains

### Before

```csharp
var x = y.GroupBy(i => i.Key.Id, i => i.Key.Version).Where(i => i.Count() > 1).ToImmutableArray();
```

### After (wrapping enabled)

```csharp
var x = y
    .GroupBy(i => i.Key.Id, i => i.Key.Version)
    .Where(i => i.Count() > 1)
    .ToImmutableArray();
```

### Before (functions on sub-prop)

```csharp
var logs = log.Entries.OrderBy(x => x.Time).ThenBy(x => x.Thread).ThenBy(x => x.LineNum).GroupBy(x => x.Group);
```

### After (wrapping enabled)

```csharp
var logs = log.Entries.OrderBy(x => x.Time)
                      .ThenBy(x => x.Thread)
                      .ThenBy(x => x.LineNum)
                      .GroupBy(x => x.Group);
```

Or, if there's a lambda in the `GroupBy`:

```csharp
var logs = log.Entries.OrderBy(x => x.Time)
                      .ThenBy(x => x.Thread)
                      .ThenBy(x => x.LineNum)
                      .GroupBy(x => {
                        if (x.Time == DateTime.MinValue)
                        {
                            return $" -Start-";
                        }
                        else
                        {
                            return $"{x.Time.ToString("yyyy-MM-dd HH:mm:ss,fff")}";
                        }
                      });
```

## Scenario 4: Parameters

### Before

```csharp
// Declaration
private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
{
    // silly implementation
}

// Caller
MyVerySillyMethod(param1, param2, param3, param4);
```

### After (Wrapping)

```csharp
// Declaration
private void MyVerySillyMethod(int param1,
    int param2, 
    int param3, 
    int param4)
{
    // silly implementation
}

// Caller
MyVerySillyMethod(param1,
    param2,
    param3,
    param4);
```

### After (Wrap & Align)

```csharp
// Declaration
private void MyVerySillyMethod(int param1,
                               int param2,
                               int param3,
                               int param4)
{
    // silly implementation
}

// Caller
MyVerySillyMethod(param1,
                  param2,
                  param3,
                  param4);
```

### After (Wrap & Align & Args On New Line)

```csharp
// Declaration
private void MyVerySillyMethod(
                                int param1,
                                int param2,
                                int param3,
                                int param4
)
{
    // silly implementation
}

// Caller
MyVerySillyMethod(
                    param1,
                    param2,
                    param3,
                    param4
);
```

### After (Wrap & Args On New Line)

```csharp
// Declaration
private void MyVerySillyMethod(
    int param1,
    int param2,
    int param3,
    int param4
)
{
    // silly implementation
}

// Caller
MyVerySillyMethod(
    param1,
    param2,
    param3,
    param4
);
```

