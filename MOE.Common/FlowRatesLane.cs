﻿using MOE.Common.Business;
using MOE.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOE.Common
{
    public class FlowRatesLane
    {
        public bool Saturation { get; } = false;
       //public double PhaseFlowRate
       //{
       //    get
       //    {
       //        return 0;
       //    }
       //}
        public double SaturationFlowRate {
            get
            {
                if (Detections.Count < 10)
                    return 0;
                var start5th = Detections[4].Timestamp;
                var start10th = Detections[9].Timestamp;
                var flowRate = 3600 / ((start10th - start5th).TotalSeconds / 5);
                return flowRate;
            }
        }
        public int DetectorChannel { get; }
        public List<Controller_Event_Log> Detections = new List<Controller_Event_Log>();

        public FlowRatesLane(List<Controller_Event_Log> events)
        {
            events = events.Where(e => e.EventCode == 82).OrderBy(e => e.Timestamp).ToList();
            if (events.Any())
            {
                DetectorChannel = events.First().EventParam;
                Detections = events;
                Saturation = events.Count >= 10;
            }
        }
    }
}