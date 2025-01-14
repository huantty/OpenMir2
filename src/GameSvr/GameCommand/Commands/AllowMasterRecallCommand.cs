﻿using GameSvr.Player;
using SystemModule.Data;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 此命令用于允许或禁止师徒传送
    /// </summary>
    [Command("AllowMasterRecall", "此命令用于允许或禁止师徒传送", 0)]
    public class AllowMasterRecallCommand : Command
    {
        [ExecuteCommand]
        public void AllowMasterRecall(PlayObject PlayObject)
        {
            PlayObject.m_boCanMasterRecall = !PlayObject.m_boCanMasterRecall;
            if (PlayObject.m_boCanMasterRecall)
            {
                PlayObject.SysMsg(CommandHelp.EnableMasterRecall, MsgColor.Blue, MsgType.Hint);
            }
            else
            {
                PlayObject.SysMsg(CommandHelp.DisableMasterRecall, MsgColor.Blue, MsgType.Hint);
            }
        }
    }
}