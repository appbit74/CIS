using System.Collections.Generic;
using CIS.Models; // เพื่อให้รู้จัก NewsArticle และ LauncherGroup

namespace CIS.ViewModels
{
    public class HomeDashboardViewModel
    {
        // ส่วนแสดงข่าวประชาสัมพันธ์
        public IEnumerable<NewsArticle> NewsArticles { get; set; }

        // ส่วนแสดงระบบงาน (App Launcher)
        public IEnumerable<LauncherGroup> LauncherGroups { get; set; }

        // Constructor เพื่อป้องกัน Null Reference Error (Optional)
        public HomeDashboardViewModel()
        {
            NewsArticles = new List<NewsArticle>();
            LauncherGroups = new List<LauncherGroup>();
        }
    }
}