using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

/*
 * FILE             :   Logger.cs
 * PROJECT          :   PROG2001 - A06 My Own Web Server
 * PROGRAMMER       :   Devin Graham
 * FIRST VERSION    :   2021-11-22
 * DESCRIPTION      :   
 *      The purpose of this file is to instantiate the logger. The
 *      resetLog method checks if a log file has been created. If it
 *      has it deletes it to reset it. The Log method takes a message
 *      to be logged, time stamps it, and saves it to a new line in the log
 *      file. The log file is created in the same directory as the exe
 * 
 */

namespace myOwnWebServer
{
    
    public static class Logger
    {
        private static readonly object sync = new object();
        static string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
        static string logName = strWorkPath + "\\myOwnWebServer.log";


        /*
         * FUNCTION     :   resetLog
         * DESCRIPTION  :   
         *      This method checks if a log file exists. If one
         *      does the system deletes the file to reset it.
         * PARAMETERS   :
         *      none
         * RETURNS      :
         *      void
         */
        public static void resetLog()
        {
            //Check if the file exists and delete it if it does
            if (File.Exists(logName))
            {
                File.Delete(logName);
            }
        }


        /*
         * FUNCTION     :   Log
         * DESCRIPTION  :   
         *      This method takes a message to be logged, timestamps it, and
         *      writes it to the log file on a newline
         * PARAMETERS   :
         *      string message  :   message to be logged
         * RETURNS      :
         *      void
         */
        public static void Log(string message)
        {
            //Lock the file for safety
            lock (sync)
            {
                StreamWriter sw = new StreamWriter(logName, true);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + message);
                sw.Close();
            }
        }
    }
}
