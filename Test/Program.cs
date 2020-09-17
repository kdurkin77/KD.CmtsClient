using System;
using System.Net;
using System.Threading.Tasks;
using KD.CmtsClient.Telnet;

namespace Test
{
    public class Program
    {
        public static async Task Main()
        {
            //values needed to communicate to the CMTS
            var ip = IPAddress.Parse("");
            var timeout = TimeSpan.FromSeconds(5.0);
            var password = "";
            var enablePassword = "";

            //value needed for ShowCableModem command
            var mac = "1212.1212.1212";

            //make the cmts telnet client using the ip and timeout
            using (ITelnetCmtsClient cmtsClient = new TelnetCmtsClient())
            {
                //connect
                await cmtsClient.ConnectAsync(ip);
                //log into the cmts, some require a password and then to be enabled with a password
                var isLoggedIn = await cmtsClient.Login(password, enablePassword, timeout).ConfigureAwait(false);
                if (!isLoggedIn)
                {
                    throw new Exception("Could Not Login");
                }

                //do the show cable modem command for the mac specified
                var result = await cmtsClient.ShowCableModem(mac, timeout).ConfigureAwait(false);
                Console.WriteLine(result);
            }
        }
    }
}