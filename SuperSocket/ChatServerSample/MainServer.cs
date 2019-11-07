using CSBaseLib;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class MainServer : AppServer<ClientSession, EFBinaryRequestInfo>
    {
        public static ChatServerOption ServerOption;
        public static SuperSocket.SocketBase.Logging.ILog MainLogger;
        SuperSocket.SocketBase.Config.IServerConfig m_Config;

        PacketProcessor MainPacketProcessor = new PacketProcessor();
        RoomManager RoomMgr = new RoomManager();

        public MainServer()
            : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
        {
            NewSessionConnected += new SessionHandler<ClientSession>(OnConnected);
            SessionClosed += new SessionHandler<ClientSession, CloseReason>(OnClosed);
            NewRequestReceived += new RequestHandler<ClientSession, EFBinaryRequestInfo>(OnPacektReceived);
        }

        public void InitConfig(ChatServerOption option)
        {
            ServerOption = option;

            m_Config = new SuperSocket.SocketBase.Config.ServerConfig
            {
                Name = option.Name,
                Ip = "Any",
                Port = option.Port,
                Mode = SocketMode.Tcp,
                MaxConnectionNumber = option.MaxConnectionNumber,
                MaxRequestLength = option.MaxRequestLength,
                ReceiveBufferSize = option.ReceiveBufferSize,
                SendBufferSize = option.SendBufferSize
            };
        }

        /// <summary>
        /// 네트워크 설정 하고, CreateComponent 매소드에서 인덱스 풀 및 방  셋팅 및 컨텐츠 핸들러 등록 합니다.
        /// AppServer.Start 매소드를 통해서 서버 통신 진행
        /// </summary>
        public void CreateStartServer()
        {
            try
            {
                bool bResult = Setup(new SuperSocket.SocketBase.Config.RootConfig(), m_Config, logFactory: new SuperSocket.SocketBase.Logging.NLogLogFactory());

                if (bResult == false)
                {
                    Console.WriteLine("[Error] 서버 네트워크 설정 실패 ㅠㅠ");
                    return;
                }
                else
                {
                    MainLogger = base.Logger;
                    MainLogger.Info("서버 초기화 성공");
                }
                CreateComponent();
                Start();

                MainLogger.Info("서버 생성 성공");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 서버 생성 실패 : {ex.ToString()}");
            }
        }

        /// <summary>
        /// 컨텐츠 패킷 등록 및 룸 설정 및 유저 풀 설정합니다.
        /// 
        /// ClientSession.CreateIndexPool 메소드를 통해 풀 등록
        /// 이 풀은 여러 스레드에서 동시 접근할 수 있어서 동기화 처리가 필요합니다.
        /// 그래서 Thread에서 안전한 ConcurrentBag 자료구조를 이용해서 사용했습니다.
        /// 
        /// RooMgr를 통해 Config에서 설정한 룸 개수만큼 룸 생성 및 초기화 List에 추가
        /// 
        /// 
        /// PacketProcessor는 컨텐츠 패킷 핸들러 등록해주고 
        /// Process 스레드를 동작해서 MsgBuffer에서 패킷 받으면 핸들링 된 함수 호출
        /// </summary>
        /// <returns></returns>
        public ERROR_CODE CreateComponent()
        {
            ClientSession.CreateIndexPool(m_Config.MaxConnectionNumber);

            Room.NetSendFunc = this.SendData;

            RoomMgr.CreateRooms();
            MainPacketProcessor = new PacketProcessor();
            MainPacketProcessor.CreateAndStart(RoomMgr.GetRoomList(), this);

            MainLogger.Info("CreateComponent - Success");
            return ERROR_CODE.NONE;
        }

        public void StopServer()
        {
            Stop();
            MainPacketProcessor.Destroy();
        }

        public bool SendData(string sessionID, byte[] sendData)
        {
            var session = GetSessionByID(sessionID);

            try
            {
                if (session == null)
                {
                    return false;
                }
                session.Send(sendData, 0, sendData.Length);
            }
            catch (Exception ex)
            {
                // TimeoutException 예외가 발생할 수 있습니다.
                MainServer.MainLogger.Error($"{ex.ToString()}, {ex.StackTrace}");

                session.SendEndWhenSendingTimeOut();
                session.Close();
            }
            return true;
        }

        public void Distribute(ServerPacketData requestPacket)
        {
            MainPacketProcessor.InsertPacket(requestPacket);
        }


        /// <summary>
        /// 연결 후 
        /// 세션 객체 : AppSession을 상속 받았습니다.
        ///  인덱스 풀에서 빼서 등록(Pop)
        /// 
        /// 연결에 대한 패킷 만든 후 패킷 프로시저에 등록
        /// </summary>
        /// <param name="session"></param>
        void OnConnected(ClientSession session)
        {
            // 옵션의 최대 연결 수를 넘으면 SuperSocket이 바로 접속을 짤라버린다. 
            // 즉 이 OnConnected 함수가 호출되지 않는다.
            session.AllocSessionIndex(); // 미리 할당 된 인덱스 정보 받는다.
            MainLogger.Info(string.Format("세션 번호 {0} 접속", session.SessionID));

            var packet = ServerPacketData.MakeNTFConnectOrDisConnectClientPacket(true, session.SessionID, session.SessionIndex);
            Distribute(packet);
        }

        /// <summary>
        /// 연결 끊는 패킷 생성 후 인덱스 반환(Push)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        void OnClosed(ClientSession session, CloseReason reason)
        {
            MainLogger.Info(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()));


            var packet = ServerPacketData.MakeNTFConnectOrDisConnectClientPacket(false, session.SessionID, session.SessionIndex);
            Distribute(packet);

            session.FreeSessionIndex(session.SessionIndex);
        }

        /// <summary>
        /// 패킷을 전송 받았을 때 
        /// 패킷 정보 등록 후 패킷 프로세서에 등록
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reqInfo"></param>
        void OnPacektReceived(ClientSession session, EFBinaryRequestInfo reqInfo)
        {
            MainLogger.Debug(string.Format("세션 번호 {0} 받은 데이터 크기: {1}, ThreadId: {2}", session.SessionID, reqInfo.Body.Length, System.Threading.Thread.CurrentThread.ManagedThreadId));

            var packet = new ServerPacketData();
            packet.SessionID = session.SessionID;
            packet.SessionIndex = session.SessionIndex;
            packet.PacketSize = reqInfo.Size;
            packet.PacketID = reqInfo.PacketID;
            packet.Type = reqInfo.Type;
            packet.BodyData = reqInfo.Body;
            Distribute(packet);
        }
    }

    class ConfigTemp
    {
        static public List<string> RemoteServers = new List<string>();
    }
}
