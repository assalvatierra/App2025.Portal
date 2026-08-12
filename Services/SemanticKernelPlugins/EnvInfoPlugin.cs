using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Globalization;

namespace Portal.Services.SemanticKernelPlugins
{
    public class EnvInfoPlugin
    {
        [KernelFunction("get_current_datetime")]
        [Description("Gets environment current Date and Time")]
        public string GetCurrentDateTime()
        {
            var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;


            string sret = $"Date: \"{System.DateTime.Now.ToLongDateString()}\""
                + $",Date Format: \"{dtfi.LongDatePattern}\""
                + $",Time:\"{System.DateTime.Now.ToLongTimeString()}\""
                + $",Time Format: \"{dtfi.LongTimePattern}\"";

            return sret;
        }


    }
}
