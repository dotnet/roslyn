Representation of Boolean Values
================================

The C# and VB compilers represent `true` (`True`) and `false` (`False`) `bool` (`Boolean`) values with the single byte values `1` and `0`, respectively, and assume that any boolean values that they are working with are restricted to being represented by these two underlying values. The ECMA 335 CLI specification permits a "true" boolean value to be represented by any nonzero value. If you use boolean values that have an underlying representation other than `0` or `1`, you can get unexpected results. This can occur in `unsafe` code in C#, or by interoperating with a language that permits other values. To avoid these unexpected results, it is the programmer's responsibility to normalize such incoming values.

See also https://github.com/dotnet/roslyn/issues/24652
