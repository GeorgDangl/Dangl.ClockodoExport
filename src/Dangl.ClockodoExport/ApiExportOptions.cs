using CommandLine;

namespace Dangl.ClockodoExport
{
    public class ApiExportOptions
    {
        [Option('t', "token", Required = true, HelpText = "Your Clockodo API Token")]
        public string ClockodoApiToken { get; set; }

        [Option('u', "user-email", Required = true, HelpText = "Email for the Clockodo account")]
        public string UserEmail { get; set; }

        [Option('f', "folder", Required = false, HelpText = "Optional base path under which to place the data export")]
        public string ExportBaseFolder { get; set; }
    }
}
