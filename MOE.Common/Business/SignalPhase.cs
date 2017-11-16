﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using MOE.Common.Models;


namespace MOE.Common.Business
{
    public class SignalPhase
    {
        public VolumeCollection Volume { get; private set; }
        public List<PlanPcd> Plans { get; private set; }
        public List<CyclePcd> Cycles { get; private set; }
        private List<Controller_Event_Log> DetectorEvents { get; set; }
        private bool GetPermissivePhase { get; }
        public Approach Approach { get; }
        public double AvgDelay => TotalDelay / TotalVolume;
        public double PercentArrivalOnGreen
        {
            get
            {
                if (TotalVolume > 0)
                {
                    return Math.Round(((TotalArrivalOnGreen / TotalVolume) * 100));
                }
                return 0;
            }
        }
        public double PercentGreen
        {
            get
            {
                if (TotalTime > 0)
                {
                    return Math.Round(((TotalGreenTime / TotalTime) * 100));
                }
                return 0;
            }
        }
        public double PlatoonRatio
        {
            get
            {
                if (TotalVolume > 0)
                {
                    return Math.Round((PercentArrivalOnGreen / PercentGreen), 2);
                }
                return 0;
            }
        }
        public double TotalArrivalOnGreen => Cycles.Sum(d => d.TotalArrivalOnGreen);
        public double TotalArrivalOnRed => Cycles.Sum(d => d.TotalArrivalOnRed);
        public double TotalArrivalOnYellow => Cycles.Sum(d => d.TotalArrivalOnYellow);
        public double TotalDelay => Cycles.Sum(d => d.TotalDelay);
        public double TotalVolume => Cycles.Sum(d=> d.TotalVolume);
        public double TotalGreenTime => Cycles.Sum(d => d.TotalGreenTime);
        public double TotalYellowTime => Cycles.Sum(d => d.TotalYellowTime);
        public double TotalRedTime => Cycles.Sum(d => d.TotalRedTime);
        public double TotalTime => Cycles.Sum(d => d.TotalTime);
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        
        public SignalPhase(DateTime startDate, DateTime endDate, Approach approach,
            bool showVolume, int binSize, int metricTypeId, bool getPermissivePhase)
        {
            StartDate = startDate;
            EndDate = endDate;
            Approach = approach;
            GetPermissivePhase = getPermissivePhase;
            GetSignalPhaseData(showVolume, binSize, metricTypeId);
        }

        public void LinkPivotAddSeconds(int seconds)
        {
            Volume = null;
            foreach (Controller_Event_Log row in DetectorEvents)
            {
                row.Timestamp = row.Timestamp.AddSeconds(seconds);
            }
            //Todo:Fix for Link Pivot
            //Plans.LinkPivotAddDetectorData(this.DetectorEvents);
        }
        
        private void GetSignalPhaseData(bool showVolume, int binSize, int metricTypeId)
        {
            GetDetectorEvents(metricTypeId);
            Cycles = CycleFactory.GetPcdCycles(StartDate, EndDate, Approach, DetectorEvents, GetPermissivePhase);
            Plans = PlanFactory.GetPcdPlans(Cycles, StartDate, EndDate, Approach);
            //GetPreemptEvents();
            if (showVolume)
            {
                SetVolume(DetectorEvents, binSize);
            }
        }

        private void GetDetectorEvents(int metricTypeId)
        {
            var celRepository = Models.Repositories.ControllerEventLogRepositoryFactory.Create();
            DetectorEvents = new List<Controller_Event_Log>();
            var detectorsForMetric = Approach.GetDetectorsForMetricType(metricTypeId);
            foreach (Models.Detector d in detectorsForMetric)
            {
                DetectorEvents.AddRange(celRepository.GetEventsByEventCodesParamWithOffset(Approach.SignalID, StartDate,
                    EndDate, new List<int> { 81 }, d.DetChannel, d.GetOffset()));
            }
        }

        //private void GetPreemptEvents()
        //{
        //    var celRepository = Models.Repositories.ControllerEventLogRepositoryFactory.Create();
        //    List<Controller_Event_Log> preemptEvents = celRepository.GetSignalEventsByEventCodes(Approach.SignalID, StartDate,
        //                    EndDate, new List<int>() { 102 });
        //    foreach (var preemptEvent in preemptEvents)
        //    {
        //        var cycle = Cycles.FirstOrDefault(c => c.StartTime <= preemptEvent.Timestamp && c.EndTime > preemptEvent.Timestamp);
        //        cycle?.AddPreempt(new DetectorDataPoint(cycle.StartTime, preemptEvent.Timestamp, cycle.GreenEvent, cycle.YellowEvent));
        //    }
        //}

        

        private void SetVolume(List<Controller_Event_Log> detectorEvents, int binSize)
        {
            Volume = new VolumeCollection(StartDate, EndDate, detectorEvents, binSize);            
        }
    }
}
