// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace TestResources
{
    public static class Documents
    {
        private static byte[] s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Documents) + ".dllx");

        private static byte[] s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Documents) + ".pdbx");

        private static byte[] s_dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref s_dll, nameof(Documents) + ".dll");

        private static byte[] s_pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref s_pdb, nameof(Documents) + ".pdb");

        public static KeyValuePair<byte[], byte[]> PortableDllAndPdb => new KeyValuePair<byte[], byte[]>(PortableDll, PortablePdb);
        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }

    public static class Scopes
    {
        private static byte[] s_dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref s_dll, nameof(Scopes) + ".dll");

        private static byte[] s_pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref s_pdb, nameof(Scopes) + ".pdbx");

        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }

    public static class Async
    {
        private static byte[] s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Async) + ".dllx");

        private static byte[] s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Async) + ".pdbx");

        private static byte[] s_dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref s_dll, nameof(Async) + ".dll");

        private static byte[] s_pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref s_pdb, nameof(Async) + ".pdb");

        public static KeyValuePair<byte[], byte[]> PortableDllAndPdb => new KeyValuePair<byte[], byte[]>(PortableDll, PortablePdb);
        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }

    public static class MethodBoundaries
    {
        private static byte[] s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(MethodBoundaries) + ".dllx");

        private static byte[] s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(MethodBoundaries) + ".pdbx");

        private static byte[] s_dll;
        public static byte[] Dll => ResourceLoader.GetOrCreateResource(ref s_dll, nameof(MethodBoundaries) + ".dll");

        private static byte[] s_pdb;
        public static byte[] Pdb => ResourceLoader.GetOrCreateResource(ref s_pdb, nameof(MethodBoundaries) + ".pdb");

        public static KeyValuePair<byte[], byte[]> PortableDllAndPdb => new KeyValuePair<byte[], byte[]>(PortableDll, PortablePdb);
        public static KeyValuePair<byte[], byte[]> DllAndPdb => new KeyValuePair<byte[], byte[]>(Dll, Pdb);
    }
}
