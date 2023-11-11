/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

 
using System;
using System.Runtime.InteropServices;

namespace mlibakm
{
    public class DisposableGCHandle : IDisposable
    {
        public GCHandle Handle;

        public DisposableGCHandle(GCHandle handle)
        {
            Handle = handle;
        }

        protected virtual void Dispose(bool disposing)
        {
            Handle.Free();
        }

        ~DisposableGCHandle()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
