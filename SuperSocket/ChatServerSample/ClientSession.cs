using SuperSocket.SocketBase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ChatServerSample
{
    class ClientSession : AppSession<ClientSession, EFBinaryRequestInfo>
    {
        static public int MaxSessionCount { get; private set; } = 0;
        /// <summary>
        /// 스레드로 부터 안전한 정렬되지 않는 개체 컬렉션을 나타냅니다.
        /// </summary>
        static ConcurrentBag<int> IndexPool = new ConcurrentBag<int>();

        public int SessionIndex { get; private set; } = -1;

        public static void CreateIndexPool(int maxCount)
        {
            for (int i = 0; i < maxCount; ++i)
            {
                IndexPool.Add(i);
            }
            MaxSessionCount = maxCount;
        }

        public static int PopIndex()
        {
            if (IndexPool.TryTake(out var result))
            {
                return result;
            }
            return -1;
        }

        public static void PushIndex(int index)
        {
            if (index >= 0)
            {
                IndexPool.Add(index);
            }
        }

        public void AllocSessionIndex()
        {
            SessionIndex = PopIndex();
        }

        public void FreeSessionIndex(int index)
        {
            PushIndex(index);
        }

    }
}
