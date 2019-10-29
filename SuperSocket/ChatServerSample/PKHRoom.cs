using CSBaseLib;
using MessagePack;
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
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_ENTER, RequestRoomEnter);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_CHAT, RequestChat);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_LEAVE, RequestLeave);
            packetHandlerMap.Add((int)PACKETID.NTF_IN_ROOM_LEAVE, NotifyLeaveInternal);
        }
        
        public void NotifyLeaveInternal(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug($"NotifyLeaveInternal. SessionIndex: {sessionIndex}");

            var reqData = MessagePackSerializer.Deserialize<PKTInternalNtfRoomLeave>(packetData.BodyData);

            LeaveRoomUser(sessionIndex, reqData.RoomNumber);
        }

        public void RequestLeave(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("RequestLeave");

            try 
            {
                var user = UserMgr.GetUser(sessionIndex);
                if (user == null)
                {
                    return;
                }

                if (LeaveRoomUser(sessionIndex, user.RoomNumber) == false)
                {
                    return;
                }


            }
            catch (Exception e)
            {

            }
        }

        bool LeaveRoomUser(int sessionIndex , int roomNumber)
        {
            MainServer.MainLogger.Debug($"LeaveRoomUser. SessionIndex:{sessionIndex}");
            var room = GetRoom(roomNumber);
            if (room == null)
            {
                return false;
            }

            var roomUser = room.GetUser(sessionIndex);
            if (roomUser == null)
            {
                return false;
            }

            var user = UserMgr.GetUser(sessionIndex);
            user.EnteredRoom(-1);// 룸 초기화
            var userID = roomUser.UserID;
            room.RemoveUser(roomUser);
            room.NotifyPacketLeaveUser(userID);
            return true;
        }

        public void RequestChat(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("Room RequestChat");

            try
            {
                var roomObject = CheckRoomAndRoomUser(sessionIndex);

                if (roomObject.Item1 == false)
                {
                    return;
                }
                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomChat>(packetData.BodyData);
                var notifyPacket = new PKTNtfRoomChat()
                {
                    UserID = roomObject.Item3.UserID,
                    ChatMessage = reqData.ChatMessage
                };

                var Body = MessagePackSerializer.Serialize(notifyPacket);
                var sendData = PacketToBytes.Make(PACKETID.NTF_ROOM_CHAT, Body);
                roomObject.Item2.Broadcast(-1, sendData);
                MainServer.MainLogger.Debug("Room RequestChat - Success");

            }
            catch(Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        (bool, Room, RoomUser) CheckRoomAndRoomUser(int userNetSessionIndex)
        {
            var user = UserMgr.GetUser(userNetSessionIndex);
            if (user == null)
            {
                return (false, null, null);
            }

            var roomNumber = user.RoomNumber;
            var room = GetRoom(roomNumber);

            if (room == null)
            {
                return (false, null, null);
            }

            var roomUser = room.GetUser(userNetSessionIndex);

            if (roomUser == null)
            {
                return (false, room, null);
            }

            return (true, room, roomUser);

        }

        public void RequestRoomEnter(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("RequestRoomEnter");

            try
            {
                var user = UserMgr.GetUser(sessionIndex);

                if (user == null || user.IsConfirm(sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                    return;
                }

                if (user.IsStateRoom())
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID);
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomEnter>(packetData.BodyData);
                var room = GetRoom(reqData.RoomNumber);

                if (room == null)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID);
                    return;
                }

                if (room.AddUser(user.ID() , sessionIndex, sessionID) == false )
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID);
                    return;
                }

                user.EnteredRoom(reqData.RoomNumber);
                room.NotifyPacketUserList(sessionID);
                room.NotifyPacketNewUser(sessionIndex, user.ID());

                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID);
                MainServer.MainLogger.Debug("RequestEnterInternal - Success");

            }
            catch(Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        void ResponseEnterRoomToClient(ERROR_CODE errorCode , string sessionID)
        {
            var resRoomEnter = new PKTResRoomEnter()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_ENTER, bodyData);
            ServerNetwork.SendData(sessionID, sendData);
        }


        Room GetRoom(int roomNumber)
        {
            var index = roomNumber - StartRoomNumber;
            if (index < 0 || index >= RoomList.Count)
            {
                return null;
            }
            return RoomList[index];
        }

    }
}
