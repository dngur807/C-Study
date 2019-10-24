using System;
using System.Collections.Generic;
using System.Text;

namespace FreeNet
{
    public class CUserToken
    {
        // 바이트를 패킷 형식으로 해석해주는 해석기.
        CMessageResolver message_resolver;

        void on_receive(byte[] buffer , int offset , int transferred)
        {
            this.message_resolver.on_receive(buffer, offset, transferred);
        }
    }
}
