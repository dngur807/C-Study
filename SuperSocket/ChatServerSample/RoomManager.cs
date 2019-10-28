using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class RoomManager
    {
        List<Room> RoomList = new List<Room>();

        public void CreateRooms()
        {
            var maxRoomCount = MainServer.ServerOption.RoomMaxCount;
            var startNumber = MainServer.ServerOption.RoomStartNumber;
            var maxUserCount = MainServer.ServerOption.RoomMaxUserCount;

            for (int i = 0; i < maxRoomCount; ++i)
            {
                var roomNumber = (startNumber + i);
                var room = new Room();
                room.Init(i, roomNumber, maxUserCount);

                RoomList.Add(room);
            }
        }

        public List<Room> GetRoomList() { return RoomList; }

    }
}
