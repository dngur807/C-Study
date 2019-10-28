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

        public static Func<string, byte[], bool> NetSendFunc;

       // List<RoomUser> UserList = new List<RoomUser>();
        public void Init(int index , int number , int maxUserCount)
        {
            Index = index;
            Number = number;
            MaxUserCount = maxUserCount;
        }
    }
}
