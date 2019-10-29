using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace VirusWarGameServer
{
    /// <summary>
    /// 게임 방 하나를 구성합니다. 게임의 로직이 처리되는 핵심 클래스 입니다.
    /// </summary>
    class CGameRoom
    {
        enum PLAYER_STATE : byte
        {
            // 방에 막 입장한 상태
            ENTERED_ROOM,
            // 로딩을 완료한 상태
            LOADING_COMPLETE
        }

        // 게임을 진행하는 플레이어 1P 2P가 존재합니다.
        List<CPlayer> players;

        // 플레이어들의 상태를 관리하는 변수
        Dictionary<byte, PLAYER_STATE> player_state;

        // 게임 보드판.
        List<short> gameboard;

        // 현재 턴을 진행하고 있는 플레이어 인덱스
        byte current_turn_player;

        /// <summary>
        /// 매칭이 성사된 플레이어들이 게임에 입장합니다.
        /// </summary>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
        public void enter_gameroom(CGameUser user1, CGameUser user2)
        {
            // 플레이어들을 생성하고 각각 1번, 2번 인덱스를 부여해 줍니다.
            CPlayer player1 = new CPlayer(user1, 1);
            CPlayer player2 = new CPlayer(user2, 2);

            this.players.Clear();
            this.players.Add(player1);
            this.players.Add(player2);

            // 플레이어들의 초기 상태를 지정해 준다.  
            this.player_state.Clear();
            change_playerstate(player1, PLAYER_STATE.ENTERED_ROOM);
            change_playerstate(player2, PLAYER_STATE.ENTERED_ROOM);

            // 로딩 시작메시지 전송.  
            CPacket msg = CPacket.create((Int16)PROTOCOL.START_LOADING);
            broadcast(msg);

        }

        /// <summary>
        /// 클라이언트에서 로딩을 완료한 후 요청함
        /// 이 요청이 들어오면 게임을 시작해도 좋다는 뜻입니다.
        /// </summary>
        /// <param name="sender"></param>
        public void loading_complete(CGameUser sender)
        {
            // 해당 플레이어를 로딩완료 상태로 변경
            change_playerstate(players, PLAYER_STATE.LOADING_COMPLETE);

            // 모든 유저가 준비 상태인지 체크합니다.
            if (!allplayers_ready(PLAYER_STATE.LOADING_COMPLETE))
            {
                // 아직 준비가 안된 유저가 있다면 대기합니다.
                return;
            }
            // 모두 준비 되었다면 게임을 시작합니다/
            battle_start();
        }

        /// <summary>
        /// 게임을 시작합니다.
        /// </summary>
        void battle_start()
        {
            // 게임을 새로 시작할 때 마다 초기화해줘야 할 것들
            reset_gamedata();

            // 게임 시작 메시지 전송
            CPacket msg = CPacket.create((short)PROTOCOL.GAME_START);

            // 플레이어들의 세균 위치 전송
            msg.push((byte)this.players.Count);
            this.players.ForEach(player =>
            {
                msg.push(player.player_index);      // 누구인지 구분하기 위한 플레이어 인덱스.  

                // 플레이어가 소지한 세균들의 전체 개수.  
                byte cell_count = (byte)player.viruses.Count;
                msg.push(cell_count);
                // 플레이어의 세균들의 위치정보.  
                player.viruses.ForEach(position => msg.push(position));
            });
            // 첫 턴을 진행할 플레이어 인덱스.  
            msg.push(this.current_turn_player);
            broadcast(msg);
        }

        /// <summary>
        /// 게임 데이터를 초기화 한다
        /// 게임을 새로 시작할 때 마다 초기화 해줘야 할 것들을 넣는다.
        /// </summary>
        void reset_gamedata()
        {
            // 보드판 데이터 초기화.  
            for (int i = 0; i < this.gameboard.Count; ++i)
            {
                this.gameboard[i] = 0;
            }
            // 1번 플레이어의 세균은 왼쪽위(0,0), 오른쪽위(0,7) 두군데에 배치한다.  
            put_virus(1, 0, 0);
            put_virus(1, 0, 7);
            // 2번 플레이어는 세균은 왼쪽아래(7,0), 오른쪽아래(7,7) 두군데에 배치한다.  
            put_virus(2, 7, 0);
            put_virus(2, 7, 7);

            // 턴 초기화.  
            this.current_turn_player = 1;   // 1P부터 시작.
        }

        /// <summary>
        /// 클라이언트의 이동 요청
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="begin_pos"></param>
        /// <param name="target_pos"></param>
        public void moveing_req(CGameUser sender, byte begin_pos, byte target_pos)
        {
            // sender 차례인지 체크
            if (this.current_turn_player != sender.player_index)
            {
                // 체크 이유 : 현재 자신의 차례가 아님에도 불구하고 이동 요청을 보내면
                // 게임 턴이 엉망이 됩니다.
                return;
            }

            if (this.gameboard[begin_pos] != sender.player_index)
            {
                // begin_pos 에 sender의 캐릭터가 존재하는지 체크
                // 체크 이유 : 없는 캐릭터를 이동하려고 하면 당연히 안되겠죠?
            }

            // 목적지는 0으로 설정된 빈 공간이어야 한다.
            // 다른 세균이 자리하고 있는 곳으로는 이동할 수 없습니다.
            if (this.gameboard[target_pos] != 0)
            {
                // target_pos 가 이동 또는 복제 가능한 범위인지 체크
                // 체크 이유 : 이동할 수 없는 범위로는 갈 수 없도록 처리
                return;// 목적지에 다른 세균 존재
            }
            // 모든 체크가 정상이라면 이동 처리
            short distance = CHelper.get_distance(begin_pos, target_pos);
            if (distance > 2)
            {
                // 2칸을 초과하는 거리는 이동할 수 없다.
                return;
            }

            if (distance <= 0)
            {
                // 자기 자신의 위치로는 이동할 수 없습니다.
                return;
            }

            // 모든 체크가 정상이라면 이동을 처리합니다.
            if (distance == 1)// 이동 거리가 한칸일 경우에는 복제를 수행한다.
            {
                put_virus(sender.player_index, target_pos);
            }
            else if (distance == 2)
            {
                // 이전 위치에 있는 세균은 삭제합니다.
                remove_virus(sender.player_index, begin_pos);
                // 새로운 위치에 세균을 놓는다.
                put_virus(sender.player_index, target_pos);
            }


            // 세균을 이동하여 로직 처리를 수행 -> 전염시킬 상대방 세균이 있다면 룰에 맞게
            // 전염시킨다.

            // 목적지를 기준으로 주위에 존재하는 상대방 세균을 감염시켜 같은 편으로 만든다.
            CPlayer opponent = get_opponent_player();
            infect(target_pos, sender, opponent);


            // 최종 결과를 모든 클라이언트에게 전송 합니다.
            CPacket msg = CPacket.create((short)PROTOCOL.PLAYER_MOVED);
            msg.push(sender.player_index);  // 누가
            msg.push(begin_pos);            // 어디서
            msg.push(target_pos);           // 어디로 이동했는지
            broadcast(msg);
            /*  // 턴을 종료 합니다.
              turn_end();*/
        }

        /// <summary>
        /// 턴을 종료합니다 .게임이 끝났는지 확인 과정 수행
        /// </summary>
        void turn_end()
        {
            // 보드판 상태를 확인하여 게임이 끝났는지 검사합니다.
            // 아직 게임이 끝나지 않았다면 다음 플레이어로 턴을 넘긴다.
            if (!CHelper.can_play_more(this.gameboard, get_currnet_player(), this.players))
            {
                return;
            }

            // 아직 게임이 끝나지 않았다면 다음 플레이어로 턴을 넘긴다.
            if (this.current_turn_player < this.players.Count - 1)
            {
                ++this.current_turn_player;
            }
            else
            {
                // 다시 첫번째 플레이어의 턴으로 만들어 준다.
                this.current_turn_player = this.players[0].player_index;
            }

            // 턴을 시작한다.
            start_turn();
        }

        /// <summary>
        /// 클라이언트에서 턴 연출이 모두 완료 되었을 때 호출됩니다.
        /// </summary>
        /// <param name="sender"></param>
        public void turn_finished(CPlayer sender)
        {
            change_playerstate(sender, PLAYER_STATE.CLIENT_TURN_FINISHED);

            if (!allplayers_ready(PLAYER_STATE.CLIENT_TURN_FINISHED))
            {
                return;
            }

            // 턴을 넘깁니다.
            turn_end();
        }

        /// <summary>
        /// 방에 모든 유저에게 메시지 전송
        /// </summary>
        /// <param name="msg"></param>
        void broadcast(CPacket msg)
        {
            this.players.ForEach(player => player.send(msg));
            CPacket.destroy(msg);
        }


        /// <summary>  
        /// 플레이어의 상태를 변경한다.  
        /// </summary>  
        /// <param name="player"></param>  
        /// <param name="state"></param>  
        void change_playerstate(CPlayer player, PLAYER_STATE state)
        {
            if (this.player_state.ContainsKey(player.player_index))
            {
                this.player_state[player.player_index] = state;
            }
            else
            {
                this.player_state.Add(player.player_index, state);
            }
        }

        /// <summary>
        /// 모든 플레이어가 특정 상태가 되었는지 판단
        /// 모든 플레이어가 같은 상태에 있다면 true 한명이라도 다른 상태에 있다면 false를 리턴
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        bool allplayers_ready(PLAYER_STATE state)
        {
            foreach (KeyValuePair<byte, PLAYER_STATE> kvp in this.player_state)
            {
                if (kvp.Value != state)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 보드판에 플레이어의 세균을 배치합니다.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="position"></param>
        void put_virus(byte player_index, short position)
        {
            this.gameboard[position] = player_index;
            get_player(player_index).add_cell(position);
        }

        /// <summary>
        /// 배치된 세균을 삭제 합니다.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="position"></param>
        void remove_virus(byte player_index, short position)
        {
            this.gameboard[position] = 0;
            get_player(player_index).remove_cell(position);
        }

        /// <summary>  
        /// 상대방의 세균을 감염 시킨다.  
        /// </summary>  
        /// <param name="basis_cell"></param>  
        /// <param name="attacker"></param>  
        /// <param name="victim"></param>  
        public void infect(short basis_cell, CPlayer attacker, CPlayer victim)
        {
            // 방어자의 세균중에 기준위치로 부터 1칸 반경에 있는 세균들이 감염 대상이다.  
            List<short> neighbors = CHelper.find_neighbor_cells(basis_cell, victim.viruses, 1);
            foreach (short position in neighbors)
            {
                // 방어자의 세균을 삭제한다.  
                remove_virus(victim.player_index, position);

                // 공격자의 세균을 추가하고,  
                put_virus(attacker.player_index, position);
            }
        }
    }

}
