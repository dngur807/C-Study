using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirusWarGameServer
{
    using FreeNet;
    class CPlayer
    {
        CGameUser owner;
        public byte player_index { get; private set; }

        public CPlayer(CGameUser user, byte player_index)
        {
            this.owner = user;
            this.player_index = player_index;
        }

        public void send(CPacket msg)
        {
            this.owner.send(msg);
        }
    }
}
