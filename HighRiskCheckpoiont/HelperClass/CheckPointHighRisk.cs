using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighRiskCheckpoiont.HelperClass
{
    public class CheckPointHighRisk
    {
        public int id { get; set; }
        public int checkpoint_id { get; set; }
        public int chrTimeAllowance { get; set; }
        public int chrCheckOffset { get; set; }
        public DateTime chrLastCheck { get; set; }
        public DateTime chrStartTime { get; set; }
        public bool chrActive { get; set; }
        public bool lastCheckNull { get; set; }
        public bool startCheckNull { get; set; }
        public DateTime lastPatrolDateTime { get; set; }
        public double gmtOffset { get; set; }
        public bool anyMissedVisitData { get; set; }

        public CheckPointHighRisk(object Id, object Checkpoint_id, object ChrTimeAllowance, object ChrCheckOffset, object ChrStartTime, object ChrLastCheck, object ChrActive, object GmtOffset)
        {
            id = Convert.ToInt32(Id);
            checkpoint_id = Convert.ToInt32(Checkpoint_id);
            chrTimeAllowance = Convert.ToInt32(ChrTimeAllowance);
            chrCheckOffset = Convert.ToInt32(ChrCheckOffset);
            chrStartTime = Convert.ToDateTime(ChrStartTime);
            chrLastCheck = Convert.ToDateTime(ChrLastCheck);
            chrActive = Convert.ToBoolean(ChrActive);
            gmtOffset = Convert.ToDouble(GmtOffset);
        }

        public CheckPointHighRisk(object Id, object Checkpoint_id, object ChrTimeAllowance, object ChrCheckOffset, bool ChrStartCheckNull, bool ChrLastCheckNull, object ChrActive, object GmtOffset)
        {
            id = Convert.ToInt32(Id);
            checkpoint_id = Convert.ToInt32(Checkpoint_id);
            chrTimeAllowance = Convert.ToInt32(ChrTimeAllowance);
            chrCheckOffset = Convert.ToInt32(ChrCheckOffset);
            lastCheckNull = ChrLastCheckNull;
            startCheckNull = ChrStartCheckNull;
            chrActive = Convert.ToBoolean(ChrActive);
            gmtOffset = Convert.ToDouble(GmtOffset);
        }
    }
}
