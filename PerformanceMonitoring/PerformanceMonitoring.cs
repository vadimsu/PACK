using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerformanceMonitoring
{
    public class PerformanceMonitoring
    {
        long ticksOverall;
        long startStampt;
        long callsCount;
        string Name;
        public PerformanceMonitoring(string name)
        {
            ticksOverall = 0;
            startStampt = 0;
            callsCount = 0;
            Name = name;
        }
        public void EnterFunction()
        {
            startStampt = DateTime.Now.Ticks;
            callsCount++;
        }
        public void LeaveFunction()
        {
            ticksOverall += (DateTime.Now.Ticks - startStampt);
        }
        public long GetOverallTicks()
        {
            return ticksOverall;
        }
        public long GetAverageTicks()
        {
            if (callsCount == 0)
            {
                return 0;
            }
            return (ticksOverall / callsCount);
        }
        public void Log()
        {
            LogUtility.LogUtility.LogFile(Name + " " + Convert.ToString(GetOverallTicks()) + " " + Convert.ToString(GetAverageTicks()) + " " + Convert.ToString(callsCount), LogUtility.LogLevels.LEVEL_LOG_HIGH2);
        }
    }
}
