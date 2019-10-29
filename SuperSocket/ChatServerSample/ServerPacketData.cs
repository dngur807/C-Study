using CSBaseLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class ServerPacketData
    {
        public Int16 PacketSize;
        public string SessionID;
        public int SessionIndex;
        public Int16 PacketID;
        public SByte Type;
        public byte[] BodyData;

        public void Assign(string sessionID, int sessionIndex, Int16 packetID , byte[] packetBodyData)
        {
            SessionIndex = sessionIndex;
            SessionID = sessionID;
            PacketID = packetID;

            if (packetBodyData.Length > 0)
            {
                BodyData = packetBodyData;
            }

        }

        public static ServerPacketData MakeNTFConnectOrDisConnectClientPacket(bool isConnect , string sessionID , int sessionIndex)
        {
            var packet = new ServerPacketData();

            if (isConnect)
            {
                packet.PacketID = (Int32)PACKETID.NTF_IN_CONNECT_CLIENT;
            }
            else
            {
                packet.PacketID = (Int32)PACKETID.NTF_IN_DISCONNECT_CLIENT;
            }

            packet.SessionIndex = sessionIndex;
            packet.SessionID = sessionID;
            return packet;
        }
    }

}
