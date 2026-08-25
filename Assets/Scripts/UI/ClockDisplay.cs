using TMPro;
using UnityEngine;

namespace Wuziqi.UI
{
    /// <summary>锁屏样式的时钟信息：日期 + 星期 + 时钟，每秒刷新一次。</summary>
    public class ClockDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text timeText;

        private static readonly string[] WeekDays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
        private string lastStamp;

        private void Update()
        {
            var now = System.DateTime.Now;
            string stamp = now.ToString("yyyyMMddHHmm");
            if (stamp == lastStamp) return;
            lastStamp = stamp;

            if (dateText != null)
                dateText.text = $"{now.Year}年{now.Month}月{now.Day}日 {WeekDays[(int)now.DayOfWeek]}";
            if (timeText != null)
                timeText.text = now.ToString("HH:mm");
        }
    }
}
