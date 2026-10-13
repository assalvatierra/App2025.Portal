namespace Portal.Models
{
    /// <summary>
    /// Data Transfer Object for displaying VectorDB schedule information in the view.
    /// </summary>
    public class VectorDbScheduleViewDto
    {
        /// <summary>
        /// Whether a schedule is currently configured.
        /// </summary>
        public bool HasSchedule { get; set; }

        /// <summary>
        /// The next scheduled date and time for the update (formatted for display).
        /// </summary>
        public string NextScheduleFormatted { get; set; } = string.Empty;

        /// <summary>
        /// The interval display (e.g., "2 Hours", "30 Minutes").
        /// </summary>
        public string IntervalDisplay { get; set; } = string.Empty;

        /// <summary>
        /// The last update triggered time (formatted for display).
        /// </summary>
        public string? LastUpdateTriggeredFormatted { get; set; }

        /// <summary>
        /// The actual NextSchedule DateTime value for JavaScript initialization.
        /// </summary>
        public DateTime? NextScheduleDateTime { get; set; }

        /// <summary>
        /// The actual interval value (e.g., 2, 30).
        /// </summary>
        public int IntervalValue { get; set; }

        /// <summary>
        /// The actual interval unit (Minutes, Hours, Days).
        /// </summary>
        public string IntervalUnit { get; set; } = "Days";
    }
}
