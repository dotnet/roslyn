using Microsoft.Cci;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class GenerationDelta
    {
        internal static readonly ImmutableArray<int> EmptyTableSizes = ImmutableArray.Create(new int[(int)TableIndices.Count]);

        public readonly IReadOnlyDictionary<ITypeDefinition, uint> TypesAdded;
        public readonly IReadOnlyDictionary<IEventDefinition, uint> EventsAdded;
        public readonly IReadOnlyDictionary<IFieldDefinition, uint> FieldsAdded;
        public readonly IReadOnlyDictionary<IMethodDefinition, uint> MethodsAdded;
        public readonly IReadOnlyDictionary<IPropertyDefinition, uint> PropertiesAdded;
        public readonly IReadOnlyDictionary<uint, uint> EventMapAdded;
        public readonly IReadOnlyDictionary<uint, uint> PropertyMapAdded;

        public readonly ImmutableArray<int> TableEntriesAdded;

        public readonly int BlobStreamLengthAdded;
        public readonly int StringStreamLengthAdded;
        public readonly int UserStringStreamLengthAdded;
        public readonly int GuidStreamLengthAdded;

        public readonly int DataFieldOffset;

        /// <summary>
        /// Map from syntax to local variable for methods added or updated
        /// since the initial generation, indexed by method row.
        /// </summary>
        public readonly IReadOnlyDictionary<uint, ImmutableArray<EncLocalInfo>> LocalsForMethodsAddedOrChanged;

        public GenerationDelta()
        {
            this.TypesAdded = new Dictionary<ITypeDefinition, uint>();
            this.EventsAdded = new Dictionary<IEventDefinition, uint>();
            this.FieldsAdded = new Dictionary<IFieldDefinition, uint>();
            this.MethodsAdded = new Dictionary<IMethodDefinition, uint>();
            this.PropertiesAdded = new Dictionary<IPropertyDefinition, uint>();
            this.EventMapAdded = new Dictionary<uint, uint>();
            this.PropertyMapAdded = new Dictionary<uint, uint>();
            this.TableEntriesAdded = EmptyTableSizes;
            this.BlobStreamLengthAdded = 0;
            this.StringStreamLengthAdded = 0;
            this.UserStringStreamLengthAdded = 0;
            this.GuidStreamLengthAdded = 0;
            this.DataFieldOffset = 0;
            this.LocalsForMethodsAddedOrChanged = new Dictionary<uint, ImmutableArray<EncLocalInfo>>();
        }

        public GenerationDelta(
            IReadOnlyDictionary<ITypeDefinition, uint> typesAdded,
            IReadOnlyDictionary<IEventDefinition, uint> eventsAdded,
            IReadOnlyDictionary<IFieldDefinition, uint> fieldsAdded,
            IReadOnlyDictionary<IMethodDefinition, uint> methodsAdded,
            IReadOnlyDictionary<IPropertyDefinition, uint> propertiesAdded,
            IReadOnlyDictionary<uint, uint> eventMapAdded,
            IReadOnlyDictionary<uint, uint> propertyMapAdded,
            ImmutableArray<int> tableEntriesAdded,
            int blobStreamLengthAdded,
            int stringStreamLengthAdded,
            int userStringStreamLengthAdded,
            int guidStreamLengthAdded,
            int dataFieldOffset,
            IReadOnlyDictionary<uint, ImmutableArray<EncLocalInfo>> localsForMethodsAddedOrChanged)
        {
            Debug.Assert(tableEntriesAdded.Length == (int)TableIndices.Count);

            // The size of each table is the total number of entries added in all previous
            // generations after the initial generation. Depending on the table, some of the
            // entries may not be available in the current generation (say, a synthesized type
            // from a method that was not recompiled for instance)
            Debug.Assert(tableEntriesAdded[(int)TableIndices.TypeDef] >= typesAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.Event] >= eventsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.Field] >= fieldsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.Method] >= methodsAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.Property] >= propertiesAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.EventMap] >= eventMapAdded.Count);
            Debug.Assert(tableEntriesAdded[(int)TableIndices.PropertyMap] >= propertyMapAdded.Count);

            this.TypesAdded = typesAdded;
            this.EventsAdded = eventsAdded;
            this.FieldsAdded = fieldsAdded;
            this.MethodsAdded = methodsAdded;
            this.PropertiesAdded = propertiesAdded;
            this.EventMapAdded = eventMapAdded;
            this.PropertyMapAdded = propertyMapAdded;
            this.TableEntriesAdded = tableEntriesAdded;
            this.BlobStreamLengthAdded = blobStreamLengthAdded;
            this.StringStreamLengthAdded = stringStreamLengthAdded;
            this.UserStringStreamLengthAdded = userStringStreamLengthAdded;
            this.GuidStreamLengthAdded = guidStreamLengthAdded;
            this.DataFieldOffset = dataFieldOffset;
            this.LocalsForMethodsAddedOrChanged = localsForMethodsAddedOrChanged;
        }
    }
}
