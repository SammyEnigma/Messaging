using System;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{

    public static class Extensions
    {
        public static void Dispose(this Rv.Listener listener)
        {
            if (listener == null)
                return;

            try
            {
                listener.Destroy();
            }
            catch
            {
                // Dispose methods must not throw exceptions
            }
            GC.SuppressFinalize(listener);  // RV does not do this, so we have to
        }

        public static void Dispose(this Rv.Transport transport)
        {
            if (transport == null)
                return;

            try
            {
                transport.Destroy();
            }
            catch
            {
                // Dispose methods must not throw exceptions
            }
            GC.SuppressFinalize(transport);  // RV does not do this, so we have to
        }
    }
}