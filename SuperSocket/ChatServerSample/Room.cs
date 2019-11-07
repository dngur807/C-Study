using CSBaseLib;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class Room
    {
        public int Index { get; private set; }
        public int Number { get; private set; }

        int MaxUserCount = 0;

        List<RoomUser> UserList = new List<RoomUser>();
        public static Func<string, byte[], bool> NetSendFunc;

        // List<RoomUser> UserList = new List<RoomUser>();
        public void Init(int index, int number, int maxUserCount)
        {
            Index = index;
            Number = number;
            MaxUserCount = maxUserCount;
        }

        public bool AddUser(string userID, int netSessionIndex, string netSessionID)
        {
            if (GetUser(userID) != null)
            {
                return false;
            }

            var roomUser = new RoomUser();
            roomUser.Set(userID, netSessionIndex, netSessionID);
            UserList.Add(roomUser);
            return true;
        }

        public RoomUser GetUser(string UserID)
        {
            return UserList.Find(x => x.UserID == UserID);
        }

        public RoomUser GetUser(int netSessionIndex)
        {
            return UserList.Find(x => x.NetSessionIndex == netSessionIndex);

        }

        /// <summary>
        /// 룸에 접속한 유저에게 다른 사람들 정보를 제공합니다.
        /// </summary>
        /// <param name="userNetSessionID"></param>
        public void NotifyPacketUserList(string userNetSessionID)
        {
            var packet = new PKTNtfRoomUserList();
            foreach (var user in UserList)
            {
                packet.UserIDList.Add(user.UserID);
            }

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_USER_LIST, bodyData);
            NetSendFunc(userNetSessionID, sendPacket);
        }


        public void NotifyPacketNewUser(int newUserNetSessionIndex, string newUserID)
        {
            var packet = new PKTNtfRoomNewUser();
            packet.UserID = newUserID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_NEW_USER, bodyData);

            Broadcast(newUserNetSessionIndex, sendPacket);
        }

        public void Broadcast(int excludeNetSessionIndex, byte[] sendPacket)
        {
            foreach (var user in UserList)
            {
                if (user.NetSessionIndex == excludeNetSessionIndex)
                {
                    continue;
                }

                NetSendFunc(user.NetSessionID, sendPacket);
            }
        }

        public bool RemoveUser(RoomUser user)
        {
            return UserList.Remove(user);
        }
        public int CurrentUserCount()
        {
            return UserList.Count;
        }
        public void NotifyPacketLeaveUser(string userID)
        {
            if (CurrentUserCount() == 0)
            {
                return;
            }

            var packet = new PKTNtfRoomLeaveUser();
            packet.UserID = userID;
            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_LEAVE_USER, bodyData);

            Broadcast(-1, sendPacket);
        }
    }

    public class RoomUser
    {
        public string UserID { get; private set; }
        public int NetSessionIndex { get; private set; }
        public string NetSessionID { get; private set; }

        public void Set(string userID, int netSessionIndex, string netSessionID)
        {
            UserID = userID;
            NetSessionIndex = netSessionIndex;
            NetSessionID = netSessionID;
        }
    }


}
