using System.Collections.Generic;

namespace GameSvr
{
    public class TMonGenInfo
    {
        public string sMapName;
        public int nX;
        public int nY;
        public string sMonName;
        public int nRange;
        public int nCount;
        public int dwZenTime;
        public int nMissionGenRate;
        public IList<TBaseObject> CertList;
        public int CertCount;
        public object Envir;
        public int nRace;
        public int dwStartTick;
    }
}