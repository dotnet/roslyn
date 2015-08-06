// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace TestResources
{
    public static class Documents
    {
        private static byte[] _dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref _dll, nameof(Documents) + ".dll");

        private static byte[] _pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref _pdb, nameof(Documents) + ".pdbx");

        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }

    public static class Scopes
    {
        private static byte[] _dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref _dll, nameof(Scopes) + ".dll");

        private static byte[] _pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref _pdb, nameof(Scopes) + ".pdbx");

        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }

    public static class Async
    {
        private static byte[] _dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref _dll, nameof(Async) + ".dll");

        private static byte[] _pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref _pdb, nameof(Async) + ".pdbx");

        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }
}
