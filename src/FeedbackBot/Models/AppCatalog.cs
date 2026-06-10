using System;
using System.Collections.Generic;
using System.Linq;

namespace FeedbackBot.Models
{
    public static class AppCatalog
    {
        public static IReadOnlyList<AppInfo> Apps { get; } = new[]
        {
            new AppInfo
            {
                Name = "PrePurchasing",
                AppName = "Purchasing",
                Description = "Submit order requests for KFS, MyTravel, and other campus services.",
                IconPath = "~/media/icons/prepurchasing.svg"
            },
            new AppInfo
            {
                Name = "ACE",
                AppName = "Ace",
                Description = "Academic Course Evaluation makes course evaluations easier for students, faculty, and staff.",
                IconPath = "~/media/icons/ACE.svg"
            },
            new AppInfo
            {
                Name = "FeedbackBot",
                AppName = "feedbackBot",
                Description = "Collect and route user feedback directly into the GitHub issue workflow.",
                IconPath = "~/media/icons/feedbackbot.svg"
            },
            new AppInfo
            {
                Name = "Online Registration",
                AppName = "Crp",
                Description = "Manage event registration and collect fees for departments.",
                IconPath = "~/media/icons/CRP.svg"
            },
            new AppInfo
            {
                Name = "PEAKS",
                AppName = "Peaks",
                Description = "Track people, equipment, access, keys, and space.",
                IconPath = "~/media/icons/PEAKS.svg"
            },
            new AppInfo
            {
                Name = "Finjector",
                AppName = "finjector",
                Description = "Build, share, and use Aggie Enterprise chart strings.",
                IconPath = "~/media/icons/finjector.svg"
            },
            new AppInfo
            {
                Name = "Hippo",
                AppName = "hippo",
                Description = "High Performance Computing Personnel Onboarding.",
                IconPath = "~/media/icons/hippo.svg"
            },
            new AppInfo
            {
                Name = "Walter",
                AppName = "walter",
                Description = "Warehouse Analytics and Ledger Tools for Enterprise Reporting.",
                IconPath = "~/media/icons/walter.svg"
            }
        };

        public static AppInfo Get(string appName)
        {
            var app = Apps.FirstOrDefault(x => string.Equals(x.AppName, appName, StringComparison.OrdinalIgnoreCase));
            if (app != null)
            {
                return app;
            }

            return new AppInfo
            {
                Name = appName,
                AppName = appName,
                Description = "Search existing issues or submit a new piece of feedback.",
                IconPath = "~/media/logo_feedbackbot.svg"
            };
        }
    }
}
