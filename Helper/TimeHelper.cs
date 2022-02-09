using System;
using System.Globalization;

namespace IGameInstaller.Helper
{
    public class TimeHelper
    {
        public static DateTime StringToDateTime(string dateString)
        {
            return DateTime.ParseExact($"{dateString} +00:00", "yyyy-MM-dd HH:mm:ss zzz", null);
        }
        public static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy年MM月dd日HH时mm分ss秒");
        }
        public static string DateTimeFormat(DateTime dateTime)
        {
            var currentDateTime = DateTime.Now;
            var differTimeSpan = currentDateTime - dateTime;
            var differString = "";
            if (differTimeSpan.Hours != 0)
            {
                differString += $"{Math.Abs(differTimeSpan.Hours)}小时";
            }
            if (differTimeSpan.Minutes != 0)
            {
                differString += $"{Math.Abs(differTimeSpan.Minutes)}分";
            }
            differString += $"{Math.Abs(differTimeSpan.Seconds)}秒";
            var endString = differTimeSpan.TotalSeconds > 0 ? "前" : "后";
            return $"{differString}{endString}";
        }
    }
}
