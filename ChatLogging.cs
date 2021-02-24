using System;
using System.IO;
using Server;
using Server.Accounting;

namespace Server.Logging
{
    public class ChatLogging
    {
        private static StreamWriter m_Output;
        private static bool m_Enabled = true;

        public static bool Enabled{ get{ return m_Enabled; } set{ m_Enabled = value; } }

        public static StreamWriter Output{ get{ return m_Output; } }

        public static void Initialize()
        {
            if ( !Directory.Exists( "Logs" ) )
                Directory.CreateDirectory( "Logs" );

            string directory = "Logs/Chats";

            if ( !Directory.Exists( directory ) )
                Directory.CreateDirectory( directory );

            try
            {
                m_Output = new StreamWriter( Path.Combine( directory, String.Format( "{0}.log", DateTime.Now.ToLongDateString() ) ), true );

                m_Output.AutoFlush = true;

                m_Output.WriteLine( "##############################" );
                m_Output.WriteLine( "Log started on {0}", DateTime.Now );
                m_Output.WriteLine();
            }
            catch
            {
            }
        }

        public static void WriteLine( Mobile from, string text )
        {
            if ( !m_Enabled )
                return;

            try
            {
                /* The Warped-preferred logfile format. Example:
                 * 2021-02-24 08:26:27 (admin) Ran'Zul: test test
                 * 2021-02-24 08:26:29 (admin) Ran'Zul: testing
                 * 2021-02-24 08:26:34 (admin) Ran'Zul: chat log more.better now
                 */
                //string logoutput = String.Format ("{0} ({1}) {2}: {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), from.Account.Username, from.Name, text);

                /* The default RunUO CommandLog format, borrowed for the new ChatLog system. Example:
                 * 2/24/2021 8:33:43 AM: 127.0.0.1: "Ran'Zul" ('admin'): Test
                 * 2/24/2021 8:33:46 AM: 127.0.0.1: "Ran'Zul" ('admin'): Test Testing
                 * 2/24/2021 8:33:51 AM: 127.0.0.1: "Ran'Zul" ('admin'): This is default still more.better
                 */
                string logoutput = String.Format ("{0}: {1}: \"{2}\" ('{3}'): {4}", DateTime.Now, from.NetState, from.Name, from.Account.Username, text);

                // Log chat to log file
                m_Output.WriteLine ( logoutput );

                // Log chat to console
                Console.WriteLine ( logoutput );
            }
            catch
            {
            }
        }
    }
}
