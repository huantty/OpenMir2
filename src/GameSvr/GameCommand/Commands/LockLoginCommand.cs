﻿using GameSvr.Player;
using SystemModule.Data;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 锁定登录
    /// </summary>
    [Command("LockLogin", "锁定登录", 0)]
    public class LockLoginCommand : Command
    {
        [ExecuteCommand]
        public void LockLogin(PlayObject PlayObject)
        {
            if (!M2Share.Config.LockHumanLogin)
            {
                PlayObject.SysMsg("本服务器还没有启用登录锁功能!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            if (PlayObject.m_boLockLogon && !PlayObject.m_boLockLogoned)
            {
                PlayObject.SysMsg("您还没有打开登录锁或还没有设置锁密码!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            PlayObject.m_boLockLogon = !PlayObject.m_boLockLogon;
            if (PlayObject.m_boLockLogon)
            {
                PlayObject.SysMsg("已开启登录锁", MsgColor.Green, MsgType.Hint);
            }
            else
            {
                PlayObject.SysMsg("已关闭登录锁", MsgColor.Green, MsgType.Hint);
            }
        }
    }
}