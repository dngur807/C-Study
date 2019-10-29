using CSBaseLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    /// <summary>
    /// PKHandler를 상속 받아서 MainServer 객체, UserMgr 상속
    /// Room 컨텐츠에서 필요한 핸들링 작업
    /// </summary>
    class PKHRoom : PKHandler
    {
        List<Room> RoomList = null;
        int StartRoomNumber;

        public void SetRoomList(List<Room> roomList)
        {
            RoomList = roomList;
            StartRoomNumber = roomList[0].Number;
        }

        /// <summary>
        /// 핸들링 함수 매핑 
        /// </summary>
        /// <param name="packetHandlerMap"></param>
        public void RegistPacketHandler(Dictionary<int , Action<ServerPacketData>> packetHandlerMap)
        {
          /*  packetHandlerMap.Add((int)PACKETID.REQ_ROOM_ENTER, RequestRoomEnter);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_LEAVE, RequestLeave);
            packetHandlerMap.Add((int)PACKETID.NTF_IN_ROOM_LEAVE, NotifyLeaveInternal);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_CHAT, RequestChat);*/
        }

    }
}
