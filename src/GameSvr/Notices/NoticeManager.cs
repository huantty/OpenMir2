using SystemModule.Common;

namespace GameSvr.Notices
{
    public class NoticeManager
    {
        private readonly NoticeMsg[] NoticeList = new NoticeMsg[100];

        public NoticeManager()
        {
            for (var i = 0; i < NoticeList.Length; i++)
            {
                NoticeList[i].sMsg = string.Empty;
                NoticeList[i].sList = null;
            }
        }

        public void LoadingNotice()
        {
            for (var i = 0; i < NoticeList.Length; i++)
            {
                if (string.IsNullOrEmpty(NoticeList[i].sMsg))
                {
                    continue;
                }
                var fileName = Path.Combine(M2Share.BasePath, M2Share.Config.NoticeDir, $"{NoticeList[i].sMsg}.txt");
                if (!File.Exists(fileName)) continue;
                try
                {
                    if (NoticeList[i].sList == null)
                    {
                        NoticeList[i].sList = new StringList();
                    }
                    NoticeList[i].sList.LoadFromFile(fileName);
                }
                catch
                {
                    M2Share.Log.LogError("Error in loading notice text. file name is " + fileName);
                }
            }
        }

        public void GetNoticeMsg(string sStr, IList<string> LoadList)
        {
            var bo15 = true;
            for (var i = 0; i < NoticeList.Length; i++)
            {
                if (string.Compare(NoticeList[i].sMsg, sStr, StringComparison.OrdinalIgnoreCase) != 0) continue;
                if (NoticeList[i].sList != null)
                {
                    for (var j = 0; j < NoticeList[i].sList.Count; j++)
                    {
                        LoadList.Add(NoticeList[i].sList[j]);
                    }
                }
                bo15 = false;
            }
            if (!bo15)
            {
                return;
            }
            for (var i = 0; i < NoticeList.Length; i++)
            {
                if (string.IsNullOrEmpty(NoticeList[i].sMsg))
                {
                    var fileName = Path.Combine(M2Share.BasePath, M2Share.Config.NoticeDir, sStr + ".txt");
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            if (NoticeList[i].sList == null)
                            {
                                NoticeList[i].sList = new StringList();
                            }
                            NoticeList[i].sList.LoadFromFile(fileName, true);
                            for (var j = 0; j < NoticeList[i].sList.Count; j++)
                            {
                                LoadList.Add(NoticeList[i].sList[j]);
                            }
                        }
                        catch (Exception)
                        {
                            M2Share.Log.LogError("Error in loading notice text. file name is " + fileName);
                        }
                        NoticeList[i].sMsg = sStr;
                        break;
                    }
                }
            }
        }
    }
}