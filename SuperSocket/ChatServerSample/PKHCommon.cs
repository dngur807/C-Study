using CSBaseLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class PKHCommon : PKHandler
    {
        public void RegistPacketHandler(Dictionary<int , Action<ServerPacketData>> packetHandlerMap)
        {
            packetHandlerMap.Add((int)PACKETID.NTF_IN_CONNECT_CLIENT, NotifyInConnectClient);
            packetHandlerMap.Add((int)PACKETID.NTF_IN_DISCONNECT_CLIENT, NotifyInDisConnectClient);
            packetHandlerMap.Add((int)PACKETID.REQ_LOGIN, RequestLogin);
        }

    }
}
