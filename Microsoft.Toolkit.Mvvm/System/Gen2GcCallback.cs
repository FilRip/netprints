using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed class Gen2GcCallback : CriticalFinalizerObject
    {
        private readonly Action<object> callback;

        private GCHandle handle;

        private Gen2GcCallback(Action<object> callback, object target)
        {
            this.callback = callback;
            handle = GCHandle.Alloc(target, GCHandleType.Weak);
        }

        public static void Register(Action<object> callback, object target)
        {
            _ = new Gen2GcCallback(callback, target);
        }

        ~Gen2GcCallback()
        {
            object target = handle.Target;
            if (target != null)
            {
                try
                {
                    callback(target);
                }
                catch
                {
                }
                GC.ReRegisterForFinalize(this);
            }
            else
            {
                handle.Free();
            }
        }
    }
}
