using Portal.Models;

namespace Portal.Services.VectorSearch.Models
{
    /// <summary>
    /// Extension methods for VectorDbScheduleSettings.
    /// </summary>
    public static class VectorDbScheduleSettingsExtensions
    {
        /// <summary>
        /// Maps VectorDbScheduleSettings to VectorDbScheduleViewDto for display in views.
        /// </summary>
        /// <param name="settings">The VectorDbScheduleSettings to map</param>
        /// <returns>A VectorDbScheduleViewDto with formatted display values</returns>
        public static VectorDbScheduleViewDto MapTo(this VectorDbScheduleSettings? settings)
        {
            var viewDto = new VectorDbScheduleViewDto();

            if (settings != null)
            {
                viewDto.HasSchedule = true;
                viewDto.NextScheduleFormatted = settings.NextSchedule.ToString("yyyy-MM-dd HH:mm:ss");
                viewDto.NextScheduleDateTime = settings.NextSchedule;
                viewDto.IntervalDisplay = $"{settings.IntervalValue} {settings.IntervalUnit}";
                viewDto.IntervalValue = settings.IntervalValue;
                viewDto.IntervalUnit = settings.IntervalUnit;

                if (settings.LastUpdateTriggered.HasValue)
                {
                    viewDto.LastUpdateTriggeredFormatted = settings.LastUpdateTriggered.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                viewDto.HasSchedule = false;
            }

            return viewDto;
        }
    }
}
