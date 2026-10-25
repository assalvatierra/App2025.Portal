namespace Portal.Services.VectorSearch.Models
{
    /// <summary>
    /// Represents the scheduling configuration for VectorDB collection and index refreshes.
    /// </summary>
    public class VectorDbScheduleSettings
    {
        /// <summary>
        /// The next scheduled date and time for the update to occur.
        /// </summary>
        public DateTime NextSchedule { get; set; }

        /// <summary>
        /// The numeric value for the interval between updates (e.g., 1, 2, 30).
        /// </summary>
        public int IntervalValue { get; set; }

        /// <summary>
        /// The unit of the interval: "Minutes", "Hours", or "Days".
        /// </summary>
        public string IntervalUnit { get; set; } = "Days";

        /// <summary>
        /// The date and time the settings were last modified.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// The date and time the last update was triggered (either by scheduler or manual).
        /// </summary>
        public DateTime? LastUpdateTriggered { get; set; }
    }
}
