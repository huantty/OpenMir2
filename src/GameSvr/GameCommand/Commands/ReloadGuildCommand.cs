﻿using GameSvr.Player;
using SystemModule.Data;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 重新读取行会
    /// </summary>
    [Command("ReloadGuild", "重新读取指定行会", 10)]
    public class ReloadGuildCommand : Command
    {
        [ExecuteCommand]
        public void ReloadGuild(string[] @Params, PlayObject PlayObject)
        {
            if (@Params == null)
            {
                return;
            }
            var sParam1 = string.Empty;
            if (@Params.Length > 0)
            {
                sParam1 = @Params.Length > 0 ? @Params[0] : "";
                if (string.IsNullOrEmpty(sParam1))
                {
                    PlayObject.SysMsg(string.Format(CommandHelp.GameCommandParamUnKnow, this.GameCommand.Name, CommandHelp.GameCommandReloadGuildHelpMsg), MsgColor.Red, MsgType.Hint);
                    return;
                }
            }
            if (M2Share.ServerIndex != 0)
            {
                PlayObject.SysMsg(CommandHelp.GameCommandReloadGuildOnMasterserver, MsgColor.Red, MsgType.Hint);
                return;
            }
            var Guild = M2Share.GuildMgr.FindGuild(sParam1);
            if (Guild == null)
            {
                PlayObject.SysMsg(string.Format(CommandHelp.GameCommandReloadGuildNotFoundGuildMsg, sParam1), MsgColor.Red, MsgType.Hint);
                return;
            }
            Guild.LoadGuild();
            PlayObject.SysMsg(string.Format(CommandHelp.GameCommandReloadGuildSuccessMsg, sParam1), MsgColor.Red, MsgType.Hint);
            // UserEngine.SendServerGroupMsg(SS_207, nServerIndex, sParam1);
        }
    }
}