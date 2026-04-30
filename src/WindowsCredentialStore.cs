// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Runtime.InteropServices;

namespace SimpleOps.GsxRamp
{
    internal sealed class WindowsCredentialStore : ICredentialStore
    {
        private const uint CredTypeGeneric = 1;
        private const uint PersistLocalMachine = 2;

        public string GetSecret(string key)
        {
            IntPtr credentialPtr;
            if (!CredRead(key, CredTypeGeneric, 0, out credentialPtr))
            {
                return null;
            }

            try
            {
                var credential = (NativeCredential)Marshal.PtrToStructure(credentialPtr, typeof(NativeCredential));
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                {
                    return null;
                }

                return Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }

        public void SaveSecret(string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                DeleteSecret(key);
                return;
            }

            var credential = new NativeCredential();
            credential.Type = CredTypeGeneric;
            credential.TargetName = key;
            credential.Persist = PersistLocalMachine;
            credential.CredentialBlobSize = (uint)(secret.Length * 2);
            credential.AttributeCount = 0;
            credential.Attributes = IntPtr.Zero;
            credential.TargetAlias = null;
            credential.UserName = Environment.UserName;

            IntPtr secretPtr = Marshal.StringToCoTaskMemUni(secret);
            try
            {
                credential.CredentialBlob = secretPtr;
                if (!CredWrite(ref credential, 0))
                {
                    throw new InvalidOperationException("CredWrite failed with error " + Marshal.GetLastWin32Error() + ".");
                }
            }
            finally
            {
                Marshal.ZeroFreeCoTaskMemUnicode(secretPtr);
            }
        }

        public void DeleteSecret(string key)
        {
            CredDelete(key, CredTypeGeneric, 0);
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref NativeCredential userCredential, [In] uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree([In] IntPtr cred);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }
    }
}
