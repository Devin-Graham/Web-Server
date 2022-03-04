using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

/*
 * FILE             :   Program.cs
 * PROJECT          :   PROG2001 - A06 My Own Web Server
 * PROGRAMMER       :   Devin Graham
 * FIRST VERSION    :   2021-11-22
 * DESCRIPTION      :   
 *      The purpose of this file is to instantiate the logger and the
 *      server. The program takes the command line arguments and validates them.
 *      After the server is done being instantiated the user can press any button
 *      to shutdown the server.
 * 
 */

namespace myOwnWebServer
{
    class Program
    {
        static string webRoot = null;
        static string webIP = null;
        static string webPort = null;
        static Server webServer = null;
        

        static void Main(string[] args)
        {
            //Create a new log file
            Logger.resetLog();

            //Check if arguments are correct
            if (parseArguments(args) == 0)
            {
                webServer = new Server();

                //Instantiate server thread
                Logger.Log($"[SERVER STARTED] - {webRoot}, {webIP}, {webPort}");
                object arguments = new object[3] { webRoot, webIP, webPort };
                Thread thread = new Thread(new ParameterizedThreadStart(webServer.Start));
                thread.Start(arguments);

                //Shutdown the server on button press
                Console.ReadKey(true);
                Logger.Log("[SERVER SHUTDOWN]");
                webServer.Done = true;
                thread.Join();
            }
        }



        /*
         * FUNCTION     :   parseArguments
         * DESCRIPTION  :   
         *      This function parses the command line arguments and ensures they are 
         *      correct and in the correct order
         * PARAMETERS   :
         *      string[] args : array of command line arguments
         * RETURNS      :
         *      int : 0 on success and -1 on error
         */
        static int parseArguments(string[] args)
        {
            //Check if the correct number of arguments are present
            if (args.Length == 3)
            {
                string arg0 = args[0].Split('=')[0];
                string arg1 = args[1].Split('=')[0];
                string arg2 = args[2].Split('=')[0];

                //Check if arguments are spelled correctly and are in correct order
                if (arg0 == "-webRoot" && arg1 == "-webIP" && arg2 == "-webPort")
                {
                    webRoot = args[0].Split('=')[1];
                    webIP = args[1].Split('=')[1];
                    webPort = args[2].Split('=')[1];
                    return 0;
                }
                else
                {
                    Logger.Log("[SERVER ERROR] - Arguments either in incorrect order, missing, or unrecognized");
                    return -1;
                }
            }
            else
            {
                Logger.Log("[SERVER ERROR] - Incorrect number of command line arguments");
                return -1;
            }
        }

    }
}
