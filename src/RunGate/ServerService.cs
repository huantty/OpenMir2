using System;
using System.Diagnostics;
using SystemModule;
using SystemModule.Sockets;
using SystemModule.Sockets.AsyncSocketServer;

namespace RunGate
{
    /// <summary>
    /// 客户端服务端(Mir2-RunGate)
    /// </summary>
    public class ServerService
    {
        private ISocketServer _serverSocket;
        public int NReviceMsgSize = 0;
        private long _dwProcessClientMsgTime = 0;
        
        public ServerService()
        {
            _serverSocket = new ISocketServer(20, 2048);
            _serverSocket.OnClientConnect += ServerSocketClientConnect;
            _serverSocket.OnClientDisconnect += ServerSocketClientDisconnect;
            _serverSocket.OnClientRead += ServerSocketClientRead;
            _serverSocket.OnClientError += ServerSocketClientError;
            _serverSocket.Init();
        }

        public void Start()
        {
            _serverSocket.Start(GateShare.GateAddr, GateShare.GatePort);
        }

        public void Stop()
        {
            _serverSocket.Shutdown();
        }

        /// <summary>
        /// 新玩家链接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerSocketClientConnect(object sender, AsyncUserToken e)
        {
            TSessionInfo userSession;
            var sRemoteAddress = e.RemoteIPaddr;
            var nSockIdx = 0;
            //todo 新玩家链接的时候要随机分配一个可用网关客户端
            //根据配置文件有三种模式
            //1.轮询分配
            //2.总是分配到最小资源 即网关在线人数最小的那个
            //3.一直分配到一个 直到当前玩家达到配置上线，则开始分配到其他可用网关
            
            //从全局服务获取可用网关服务进行分配 
            
            //需要记录socket会话ID和链接网关

            var gateclient = GateShare.GetClientService();

            Console.WriteLine($"用户[{sRemoteAddress}]分配到游戏服务器[{gateclient.GateIdx}] Server:{gateclient.GetSocketIp()}");
            
            for (var nIdx = 0; nIdx < gateclient.SessionArray.Length; nIdx++)
            {
                userSession = gateclient.SessionArray[nIdx];
                if (userSession.Socket == null)
                {
                    userSession.Socket = e.Socket;
                    userSession.sSocData = "";
                    userSession.sSendData = "";
                    userSession.nUserListIndex = 0;
                    userSession.nPacketIdx = -1;
                    userSession.nPacketErrCount = 0;
                    userSession.boStartLogon = true;
                    userSession.boSendLock = false;
                    userSession.boSendAvailable = true;
                    userSession.boSendCheck = false;
                    userSession.nCheckSendLength = 0;
                    userSession.dwReceiveTick = HUtil32.GetTickCount();
                    userSession.sRemoteAddr = sRemoteAddress;
                    userSession.dwSayMsgTick = HUtil32.GetTickCount();
                    userSession.nSckHandle = (int) e.Socket.Handle;
                    nSockIdx = nIdx;
                    GateShare.SessionIndex.TryAdd(e.ConnectionId, nIdx);
                    GateShare.SessionCount++;
                    break;
                }
            }
            if (nSockIdx < gateclient.GetMaxSession())
            {
                gateclient.SendServerMsg(Grobal2.GM_OPEN, nSockIdx, (int) e.Socket.Handle, 0, e.RemoteIPaddr.Length,
                    e.RemoteIPaddr); //通知M2有新玩家进入游戏
                GateShare.AddMainLogMsg("开始连接: " + sRemoteAddress, 5);
                GateShare._ClientGateMap.TryAdd(e.ConnectionId, gateclient);//链接成功后建立对应关系
            }
            else
            {
                GateShare.DelSocketIndex(e.ConnectionId);
                e.Socket.Close();
                GateShare.AddMainLogMsg("禁止连接: " + sRemoteAddress, 1);
            }
        }

        private void ServerSocketClientDisconnect(object sender, AsyncUserToken e)
        {
            TSessionInfo userSession;
            var sRemoteAddr = e.RemoteIPaddr;
            var nSockIndex = GateShare.GetSocketIndex(e.ConnectionId);
            var userClinet = GateShare.GetUserClient(e.ConnectionId);
            if (userClinet != null)
            {
                if (nSockIndex >= 0 && nSockIndex < userClinet.GetMaxSession())
                {
                    userSession = userClinet.SessionArray[nSockIndex];
                    userSession.Socket = null;
                    userSession.nSckHandle = -1;
                    userSession.sSocData = "";
                    userSession.sSendData = "";
                    GateShare.SessionCount -= 1;
                    if (GateShare.boGateReady)
                    {
                        userClinet.SendServerMsg(Grobal2.GM_CLOSE, 0, (int) e.Socket.Handle, 0, 0, ""); //发送消息给M2断开链接
                        GateShare.AddMainLogMsg("断开连接: " + sRemoteAddr, 5);
                    }
                    GateShare.DelSocketIndex(e.ConnectionId);
                    GateShare.DeleteUserClient(e.ConnectionId);
                }
            }
            else
            {
                GateShare.DelSocketIndex(e.ConnectionId);
                GateShare.DeleteUserClient(e.ConnectionId);
                GateShare.AddMainLogMsg("断开链接: " + sRemoteAddr, 5);
                Debug.WriteLine($"获取用户对应网关失败 RemoteAddr:[{sRemoteAddr}] ConnectionId:[{e.ConnectionId}]");
            }
        }

        private void ServerSocketClientError(object sender, AsyncSocketErrorEventArgs e)
        {
            Console.WriteLine("客户端链接错误.");
        }

        /// <summary>
        /// 收到客户端消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="token"></param>
        private void ServerSocketClientRead(object sender, AsyncUserToken token)
        {
            var nSocketIndex = GateShare.GetSocketIndex(token.ConnectionId);
            var userClinet = GateShare.GetUserClient(token.ConnectionId);
            string sRemoteAddress = token.RemoteIPaddr;
            if (userClinet == null)
            {
                GateShare.AddMainLogMsg("非法攻击: " + token.RemoteIPaddr, 5);
                Debug.WriteLine($"获取用户对应网关失败 RemoteAddr:[{sRemoteAddress}] ConnectionId:[{token.ConnectionId}]");
                return;
            }
            try
            {
                long dwProcessMsgTick = HUtil32.GetTickCount();
                var nReviceLen = token.BytesReceived;
                var data = new byte[nReviceLen];
                Buffer.BlockCopy(token.ReceiveBuffer, token.Offset, data, 0, nReviceLen);
                var sReviceMsg = HUtil32.GetString(data, 0, data.Length);
                if (nSocketIndex >= 0 && nSocketIndex < userClinet.GetMaxSession() && !string.IsNullOrEmpty(sReviceMsg) && GateShare.boServerReady)
                {
                    if (nReviceLen > GateShare.nNomClientPacketSize)
                    {
                        var nMsgCount = HUtil32.TagCount(sReviceMsg, '!');
                        if (nMsgCount > GateShare.nMaxClientMsgCount || nReviceLen > GateShare.nMaxClientPacketSize)
                        {
                            if (GateShare.bokickOverPacketSize)
                            {
                                switch (GateShare.BlockMethod)
                                {
                                    case TBlockIPMethod.mDisconnect:
                                        break;
                                    case TBlockIPMethod.mBlock:
                                        GateShare.TempBlockIPList.Add(sRemoteAddress);
                                        CloseConnect(sRemoteAddress);
                                        break;
                                    case TBlockIPMethod.mBlockList:
                                        GateShare.BlockIPList.Add(sRemoteAddress);
                                        CloseConnect(sRemoteAddress);
                                        break;
                                }
                                GateShare.AddMainLogMsg("踢除连接: IP(" + sRemoteAddress + "),信息数量(" + nMsgCount + "),数据包长度(" + nReviceLen + ")", 1);
                                token.Socket.Close();
                            }
                            return;
                        }
                    }
                    NReviceMsgSize += sReviceMsg.Length;
                    if (GateShare.boShowSckData)
                    {
                        GateShare.AddMainLogMsg(sReviceMsg, 0);
                    }
                    var userSession = userClinet.SessionArray[nSocketIndex];
                    if (userSession.Socket == token.Socket)
                    {
                        var nPos = sReviceMsg.IndexOf("*", StringComparison.OrdinalIgnoreCase);
                        if (nPos > -1)
                        {
                            userSession.boSendAvailable = true;
                            userSession.boSendCheck = false;
                            userSession.nCheckSendLength = 0;
                            userSession.dwReceiveTick = HUtil32.GetTickCount();
                            sReviceMsg = sReviceMsg.Substring(0, nPos);
                            //sReviceMsg = sReviceMsg.Substring(nPos + 1, sReviceMsg.Length);
                        }
                        if (!string.IsNullOrEmpty(sReviceMsg) && GateShare.boGateReady) //&& !GateShare.boCheckServerFail
                        {
                            var userData = new TSendUserData();
                            userData.nSocketIdx = nSocketIndex;
                            userData.nSocketHandle = (int)token.Socket.Handle;
                            userData.sMsg = sReviceMsg;
                            userData.UserCientId = token.ConnectionId;
                            userData.UserClient = userClinet;
                            GateShare.ReviceMsgList.Writer.TryWrite(userData);
                        }
                    }
                }
                var dwProcessMsgTime = HUtil32.GetTickCount() - dwProcessMsgTick;
                if (dwProcessMsgTime > _dwProcessClientMsgTime)
                {
                    _dwProcessClientMsgTime = dwProcessMsgTime;
                }
            }
            catch
            {
                GateShare.AddMainLogMsg("[Exception] ClientRead", 1);
            }
        }

        private void CloseConnect(string sIPaddr)
        {
            var userSocket = _serverSocket.GetSocket(sIPaddr);
            userSocket?.Socket.Close();
        }
    }
}