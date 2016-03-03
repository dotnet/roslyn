#### <a name="MethodSpansBlob"></a>Method Spans Blob
Span is a quadruple of integers and a document reference:

* Start Line
* Start Column
* End Line
* End Column
* Document

The values of a span must satisfy the following constraints

* Start Line is within range [0, 0x20000000) and not equal to 0xfeefee.
* End Line is within range [0, 0x20000000) and not equal to 0xfeefee.
* Start Column is within range [0, 0x10000)
* End Column is within range [0, 0x10000)
* End Line is greater or equal to Start Line.
* If Start Line is equal to End Line then End Column is greater than Start Column.

_Method Spans Blob_ has the following structure:

    Blob ::= header span-record (span-record | document-record)*

#####header
| component        | value stored            | integer representation |
|:-----------------|:------------------------|:-----------------------|
| _InitialDocument_| Document row id         | unsigned compressed    |

#####sequence-point-record
| component      | value stored                                                 | integer representation                      |
|:---------------|:-------------------------------------------------------------|:--------------------------------------------|
| _ΔLines_       | _EndLine_ - _StartLine_                                      | unsigned compressed                         |
| _ΔColumns_     | _EndColumn_ - _StartColumn_                                  | _ΔLines_ = 0: unsigned compressed, non-zero |
|                |                                                              | _ΔLines_ > 0: signed compressed             |
| _δStartLine_   | _StartLine_ if this is the first non-hidden sequence point   | unsigned compressed                         |
|                | _StartLine_ - _PreviousNonHidden_._StartLine_ otherwise      | signed compressed                           |
| _δStartColumn_ | _StartColumn_ if this is the first non-hidden sequence point | unsigned compressed                         |
|                | _StartColumn_ - _PreviousNonHidden_._StartColumn_ otherwise  | signed compressed                           |

#####document-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _ΔLine_      | 0                                  | unsigned compressed            |
| _ΔColumn_	   | 0                                  | unsigned compressed            |
| _Document_   | Document row id                    | unsigned compressed            |

Each _span-record_ represents a single span. The span inherits the value of _Document_ property from the previous record (_span-record_ or _document-record_) or from _InitialDocument_ if it's the first span of a method body. 

The values of _Start Line_, _Start Column_, _End Line_ and _End Column_ of a span are calculated based upon the values of the previous span (if any) and the data stored in the record.
