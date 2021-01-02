using CommandLine;

namespace Dangl.ClockodoExport
{
    public class ApiExportOptions
    {
        [Option('t', "token", Required = true, HelpText = "Your Clockodo API Token")]
        public string SevDeskApiToken { get; set; }

        [Option('u', "user-email", Required = true, HelpText = "Email for the Clockodo account")]
        public string UserEmail { get; set; }
    }
}
