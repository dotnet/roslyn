Unicode Version Change in C# 6
==============================

The Roslyn compilers depend on the underlying platform for their Unicode behavior. As a practical matter, that means that the new compiler will reflect changes in the Unicode standard.

For example, the Unicode Katakana Middle Dot "・" (U+30FB) no longer works in identifiers in C# 6.
Its Unicode class was Pc (Punctuation, Connector) in Unicode 5.1 or older, but it changed to Po (Punctuation, Other) in Unicode 6.0.

See also https://github.com/ufcpp/UfcppSample/blob/master/BreakingChanges/VS2015_CS6/KatakanaMiddleDot.cs
