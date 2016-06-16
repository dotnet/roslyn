using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Serializable]
    class SerializableModuleData : ISerializable
    {
        private readonly ModuleData _moduleData;

        public SerializableModuleData(ModuleData moduleData)
        {
            _moduleData = moduleData;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_moduleData.Id), _moduleData.Id);
            info.AddValue(nameof(_moduleData.Kind), (int)_moduleData.Kind);
            info.AddByteArray(nameof(_moduleData.Image), _moduleData.Image);
            info.AddByteArray(nameof(_moduleData.Pdb), _moduleData.Pdb);
            info.AddValue(nameof(_moduleData.InMemoryModule), _moduleData.InMemoryModule);
        }

        private ModuleData(SerializationInfo info, StreamingContext context)
        {
            _moduleData.Id = (ModuleDataId)info.GetValue(nameof(Id), typeof(ModuleDataId));
            _moduleData.Kind = (OutputKind)info.GetInt32(nameof(Kind));
            _moduleData.Image = info.GetByteArray(nameof(Image));
            _moduleData.Pdb = info.GetByteArray(nameof(Pdb));
            _moduleData.InMemoryModule = info.GetBoolean(nameof(InMemoryModule));
        }
    }
}
