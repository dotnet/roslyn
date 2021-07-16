# Dynamic Analysis Metadata Format Specification (v 0.2)

### Overview
The format is based on concepts defined in the ECMA-335 Partition II metadata standard and in [Portable PDB format](https://github.com/dotnet/corefx/blob/main/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md).

## Metadata Layout

The physical layout of the Dynamic Analysis metadata blob starts with [Header](#Header), followed by [Tables](#Tables), followed by [Heaps](#Heaps). Layout of each of these three parts is defined in the following sections.

When stored in a managed PE file, the Dynamic Analysis metadata blob is embedded as a Manifest Resource (see ECMA-335 §6.2.2 and §22.24) of name ```<DynamicAnalysisData>```.

Unless stated otherwise, all binary values are stored in little-endian format.

This document uses the term _(un)signed compressed integer_ for an encoding of an (un)signed 29-bit integer as defined in ECMA §23.2.

## Header

| Offset  | Size | Field          | Description                                                    |
|:--------|:-----|:---------------|----------------------------------------------------------------|
| 0       | 4    | Signature      | 0x44 0x41 0x4D 0x44 (ASCII string: "DAMD") |
| 4       | 1    | MajorVersion   | Major version of the format (0) |
| 5       | 1    | MinorVersion   | Minor version of the format (2) |
| 6       | 4*T  | TableRowCounts | Row count (encoded as uint32) of each table in the metadata |
| 6 + T*4 | 4*H  | HeapSizes      | Size (encoded as uint32) of each heap in bytes |

The number of tables in this version of the format is T = 2. These tables are Document Table and Method Table and their sizes are stored in this order. No table may contain more than 0x1000000 rows. 

The number of heaps in this version of the format is H = 2. These heaps are GUID Heap and Blob Heap and their sizes are stored in this order. No heap may be larger than 2^19 bytes (0.5 GB). 

## <a name="Tables"></a>Tables

Entities stored in tables are referred to by _row id_ if used in a context that implies the table. The first row of the table has row id 1. If the table is not implied by the context the entity is referred to by its _token_ -- a 32-bit unsigned integer that combines the id of the table (in highest 8 bits) and the row id of the entity within that table (in lowest 24 bits). 

### <a name="DocumentTable"></a>Document Table: 0x01

The Document table has the following columns:
* _Name_ (Blob heap index of [document name blob](#DocumentNameBlob))
* _HashAlgorithm_ (Guid heap index)
* _Hash_ (Blob heap index)

### <a name="MethodTable"></a>Method Table: 0x02

The Method table has the following columns:
* _Spans_ (Blob heap index of [span blob](#SpanBlob))

## <a name="Heaps"></a>Heaps

### GUID

The encoding of GUID heap is the same as the encoding of ECMA #GUID heap defined in ECMA-335 §24.2.5.

Values stored in GUID heap are referred to by its _index_. The first value stored in the heap has index 1, the second value stored in the heap has index 2, etc.

### Blob

The encoding of Blob heap is the same as the encoding of ECMA #Blob heap defined in ECMA-335 §24.2.4.

Values stored in Blob heap are referred to by its _offset_ in the heap (the distance between the start of the heap and the first byte ot the encoded value). The first value of the heap has offset 0, size 1B, and encoded value 0x00 (it represents an empty blob).

### <a name="SpansBlob"></a>Spans Blob

_Span_ is a quadruple of integers and a document reference:

* Start Line
* Start Column
* End Line
* End Column
* Document

The values of must satisfy the following constraints

* Start Line is within range [0, 0x20000000)
* End Line is within range [0, 0x20000000)
* Start Column is within range [0, 0x10000)
* End Column is within range [0, 0x10000)
* End Line is greater or equal to Start Line.
* If Start Line is equal to End Line then End Column is greater than Start Column.

_Spans blob_ has the following structure:

    Blob ::= header span-record (span-record | document-record)*

#### header

| component          | value stored                  | integer representation |
|:-------------------|:------------------------------|:-----------------------|
| _InitialDocument_  | Document row id               | unsigned compressed    |

#### span-record

| component      | value stored                                            | integer representation                      |
|:---------------|:--------------------------------------------------------|:--------------------------------------------|
| _ΔLines_       | _EndLine_ - _StartLine_                                 | unsigned compressed                         |
| _ΔColumns_     | _EndColumn_ - _StartColumn_                             | _ΔLines_ = 0: unsigned compressed, non-zero |
|                |                                                         | _ΔLines_ > 0: signed compressed             |
| _δStartLine_   | _StartLine_ if this is the first _span-record_          | unsigned compressed                         |
|                | _StartLine_ - _PreviousSpan_._StartLine_ otherwise      | signed compressed                           |
| _δStartColumn_ | _StartColumn_ if this is the first _span-record_        | unsigned compressed                         |
|                | _StartColumn_ - _PreviousSpan_._StartColumn_ otherwise  | signed compressed                           |

Where _PreviousSpan_ is the span encoded in the previous _span-record_.

#### document-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _ΔLines_     | 0                                  | unsigned compressed            |
| _ΔColumns_   | 0                                  | unsigned compressed            |
| _Document_   | Document row id                    | unsigned compressed            |

Each _span-record_ represents a single _Span_. When decoding the blob the _Document_ property of a _Span_ is determined by the closest preceding _document-record_ and by _InitialDocument_ if there is no preceding _document-record_.

The values of _Start Line_, _Start Column_, _End Line_ and _End Column_ of a Span are calculated based upon the values of the previous Span (if any) and the data stored in the record.

- - -
**Note** This encoding is similar to encoding of [sequence points blob](https://github.com/dotnet/corefx/blob/main/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#SequencePointsBlob) in Portable PDB format.
- - -

### <a name="DocumentNameBlob"></a>Document Name Blob

_Document name blob_ is a sequence:

    Blob ::= separator part+

where

* _separator_ is a UTF8 encoded character, or byte 0 to represent an empty separator.
* _part_ is a compressed integer into the Blob heap, where the part is stored in UTF8 encoding (0 represents an empty string).

The document name is a concatenation of the _parts_ separated by the _separator_.

- - -
**Note** This encoding is the same as the encoding of [document name blob](https://github.com/dotnet/corefx/blob/main/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#DocumentNameBlob) in Portable PDB format.
- - -




