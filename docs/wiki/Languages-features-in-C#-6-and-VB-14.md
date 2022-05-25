* **Exists**: Already shipped in previous release 
* **Added**: Implemented for this release 
* **Planned**: Intended for this release 
* **No**: Not intended for this release 
* **N/A**: Not meaningful for this language

Please note that everything is still subject to change - this is a preview after all. However, we are reasonably confident about the overall feature set at this point.

[Visual Studio 2015 RC](http://visualstudio.com/en-us/downloads/visual-studio-2015-ctp-vs) has the following features. For a description of each feature see:
* [New Language Features in C# 6](https://github.com/dotnet/roslyn/blob/main/docs/wiki/New-Language-Features-in-C%23-6.md)
* [New Language Features in VB 14](https://github.com/dotnet/roslyn/blob/main/docs/wiki/New-Language-Features-in-VB-14.md)

| Feature | Example | C# | VB |
|:-----------|:------------|:------------:|:------------:|
| Auto-property initializers | `public int X { get; set; } = x;` | Added | Exists |
| Read-only auto-properties | `public int Y { get; } = y;` | Added | Added |
| Ctor assignment to getter-only autoprops | `Y = 15` | Added | Added |
| Static imports | `using static System.Console; … Write(4);` | Added | Exists |
| Index initializer | `new JObject { ["x"] = 3 }` | Added | No |
| Await in catch/finally | `try … catch { await … } finally { await … }` | Added | No |
| Exception filters | `catch(E e) when (e.Count > 5) { … }` | Added | Exists |
| Partial modules | `Partial Module M1` | N/A | Added |
| Partial interfaces | `Partial Interface I1` | Exists | Added |
| Multiline string literals | `"Hello<newline>World"` | Exists | Added |
| Year-first date literals | `Dim d = #2014-04-03#` | N/A | Added |
| Comments after implicit line continuation | `Dim addrs = From c in Customers ' comment` | N/A | Added |
| TypeOf ... IsNot ... | `If TypeOf x IsNot Customer Then …` | N/A | Added |
| Expression-bodied members | `public double Dist => Sqrt(X * X + Y * Y);` | Added | No |
| Null-conditional operators | `customer?.Orders?[5]` | Added | Added |
| String interpolation | `$"{p.Name} is {p.Age} years old."` | Added | Added |
| nameof operator | `string s = nameof(Console.Write);` | Added | Added |
| #pragma | `#Disable Warning BC40008` | Added | Added |
| Smart name resolution |    | N/A | Added | 
| Read-write props can implement read-only interface properties |   | Exists | Added |
| #Region inside methods |    | Exists | Added |
| Overloads inferred from Overrides |    | N/A | Added |
| CObj in attributes |   | Exists | Added |
| CRef and parameter name |    | Exists | Added |
| Extension Add in collection initializers |     | Added | Exists |
| Improved overload resolution |   | Added | N/A |

