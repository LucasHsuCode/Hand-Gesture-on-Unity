using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** \brief Clients provides a set of APIs for CoreServices. */
namespace Coretronic.Reality.Clients
{
    public abstract class AndroidOnConnectionListener : AndroidJavaProxy
    {
        public AndroidOnConnectionListener() : base("com.coretronic.clients.OnConnectionListener") { }

        public abstract void onConnect(bool isConnect);
    }
}
