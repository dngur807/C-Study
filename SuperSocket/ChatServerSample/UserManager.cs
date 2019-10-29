using CSBaseLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class UserManager
    {
        int MaxUserCount;
        UInt64 UserSequenceNumber = 0;

        Dictionary<int, User> UserMap = new Dictionary<int, User>();

        public void Init(int maxUserCount)
        {
            MaxUserCount = maxUserCount;
        }

        /// <summary>
        /// 유저 추가하는 함수로, 유저 꽉찬 경우나 중복 체크 후
        /// sessionIndex 키로 유저 등록
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="sessionID"></param>
        /// <param name="sessionIndex"></param>
        /// <returns></returns>
        public ERROR_CODE AddUser(string userID, string sessionID , int sessionIndex)
        {
            if (IsFullUserCount())
            {
                return ERROR_CODE.LOGIN_FULL_USER_COUNT;
            }
            if (UserMap.ContainsKey(sessionIndex))
            {
                return ERROR_CODE.ADD_USER_DUPLICATION;
            }

            ++UserSequenceNumber;
            var user = new User();
            user.Set(UserSequenceNumber, sessionID, sessionIndex, userID);
            UserMap.Add(sessionIndex, user);
            return ERROR_CODE.NONE;
        }

        public ERROR_CODE RemoveUser(int sessionIndex)
        {
            if (UserMap.Remove(sessionIndex) == false)
            {
                return ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID;
            }
            return ERROR_CODE.NONE;
        }

        public User GetUser(int sessionIndex)
        {
            User user = null;
            UserMap.TryGetValue(sessionIndex, out user);
            return user;
        }

        bool IsFullUserCount()
        {
            return MaxUserCount <= UserMap.Count;
        }

    }

    /// <summary>
    /// 유저 객체로 
    /// SequenceNumber는 UserManager에서 유저 추가할 때마다 증가 시켜 전달
    /// 
    /// </summary>
    public class User
    {
        UInt64 SequenceNumber = 0;
        string SessionID;
        int SessionIndex = -1;
        public int RoomNumber { get; private set; } = -1;
        string UserID;

        public void Set(UInt64 sequence , string sessionID , int sessionIndex , string userID)
        {
            SequenceNumber = sequence;
            SessionID = sessionID;
            SessionIndex = sessionIndex;
            UserID = userID;
        }
    }
}
