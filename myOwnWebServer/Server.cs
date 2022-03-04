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
 * FILE             :   Server.cs
 * PROJECT          :   PROG2001 - A06 My Own Web Server
 * PROGRAMMER       :   Devin Graham
 * FIRST VERSION    :   2021-11-22
 * DESCRIPTION      :   
 *      The purpose of this file is to create a listening socket and
 *      process requests. The server is single threaded so only one
 *      connection can be made at a time. When a connection is made
 *      the server processes the http request and sends an appropriate 
 *      response back to the client. The server will continue to run
 *      until the server is shutdown.
 * 
 */

namespace myOwnWebServer
{
    class Server
    {
        static string webRoot = null;
        static TcpListener server = null;
        static Socket client = null;
        public volatile bool Done;



        /*
         * FUNCTION     :   Start
         * DESCRIPTION  :   
         *      This function starts the listening socket when a connection is made
         *      the function requestHandler is called to process the request and
         *      send the appropriate response
         * PARAMETERS   :
         *      Object o    :   Object containing all command line arguments
         * RETURNS      :
         *      void
         */
        public void Start(Object o)
        {
            Array argArray = new object[3];
            argArray = (Array)o;

            webRoot = (string)argArray.GetValue(0);

            //Create a new socket listener on the ip and port specified in the command line
            try
            {

                Int32 port = Int32.Parse((string)argArray.GetValue(2));
                IPAddress ip = IPAddress.Parse((string)argArray.GetValue(1));

                server = new TcpListener(ip, port);
                server.Start();

                //Continue to listen for connections until the server shutdown call is made
                while (!Done)
                {
                    //Check for any pending connections
                    if(server.Pending() == true)
                    {
                        client = server.AcceptSocket();
                        requestHandler();
                    }
                }
            }
            catch (SocketException e)
            {
                Logger.Log("[SOCKET EXCEPTION] - Error Code: " + e.ErrorCode);
            }
            finally
            {
                server.Stop();
            }
        }



        /*
         * FUNCTION     :   requestHandler
         * DESCRIPTION  :   
         *      This function retrieves the HTTP request, makes
         *      a series of validation checks, and sends a correct
         *      response message back to the client. After the response
         *      is sent the client is closed.
         * PARAMETERS   :
         *      none
         * RETURNS      :
         *      void
         */
        public static void requestHandler()
        {

            DateTime now = DateTime.UtcNow;

            string errorCode = "200 OK";
            string date = now.ToString("ddd, d MMM yyyy HH:mm:ss") + " GMT";
            string serverInfo = ".Net FrameWork/4.7.2";
            string contentType = "text/html; charset=utf-8";
            int contentLength = 0;
            string contentBody = null;
            string errorBody = null;


            //Retrieve the request from the client, parse it, validate it, and send a response back
            try
            {
                byte[] data = new byte[1024];
                client.Receive(data, data.Length, 0);
                string request = System.Text.Encoding.ASCII.GetString(data);

                string verb = request.Split(' ')[0];
                string resourceFile = request.Split(' ')[1];

                //Log that a request has been made
                Logger.Log($"[REQUEST] - {verb}, {resourceFile}");

                string firstLine = request.Split('\r')[0];
                int firstLineCount = firstLine.Split(' ').Length;


                //Check if the request header is complete
                if(firstLineCount != 3)
                {
                    errorCode = "400 Bad Request";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                string requestDuplicate = request.ToLower();

                //Check if the Host information is in the request header
                if(requestDuplicate.Contains("\r\nhost:") == false)
                {
                    errorCode = "400 Bad Request";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                //Check for the mandatory blank line in the request
                if(request.Contains("\r\n\r\n") == false)
                {
                    errorCode = "400 Bad Request";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                //Check if the GET method is being used
                if (request.Split(' ')[0] != "GET")
                {
                    errorCode = "501 Not Implemented";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                string requestedResource = request.Split(' ')[1];
                string requestProtocol = request.Split(' ')[2];
                requestProtocol = requestProtocol.Split('\r')[0];

                //Check if the correct protocol is being used
                if (requestProtocol != "HTTP/1.1")
                {
                    errorCode = "505 HTTP Version Not Supported";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                string completeFilePath = webRoot + requestedResource;

                //Check if the file path requested exists
                if (!File.Exists(completeFilePath))
                {
                    errorCode = "404 Not Found";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

                string resourceSuffix = requestedResource.Split('.')[1];

                resourceSuffix = resourceSuffix.ToLower();


                //Send the correct response back based on the the files MIME type
                if (resourceSuffix == "html" || resourceSuffix == "htm" || resourceSuffix == "shtml" || resourceSuffix == "xhtml" || resourceSuffix == "xht"
                    || resourceSuffix == "mdoc" || resourceSuffix == "jsp" || resourceSuffix == "asp" || resourceSuffix == "aspx" || resourceSuffix == "jshtm")
                {
                    errorCode = "200 OK";
                    contentType = "text/html";
                    contentBody = File.ReadAllText(completeFilePath);
                    contentLength = contentBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, contentBody);
                    client.Close();
                    return;

                }
                else if (resourceSuffix == "txt")
                {
                    errorCode = "200 OK";
                    contentType = "text/plain";
                    contentBody = File.ReadAllText(completeFilePath);
                    contentLength = contentBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, contentBody);
                    client.Close();
                    return;
                }
                else if (resourceSuffix == "jpg" || resourceSuffix == "jpeg" || resourceSuffix == "jpe" || resourceSuffix == "jfif"
                    || resourceSuffix == "pjpeg" || resourceSuffix == "pjp")
                {
                    errorCode = "200 OK";
                    contentType = "image/jpeg";
                    byte[] imageBody = File.ReadAllBytes(completeFilePath);
                    contentLength = imageBody.Length;
                    sendImageResponse(errorCode, contentType, contentLength, serverInfo, date, imageBody);
                    client.Close();
                    return;

                }
                else if (resourceSuffix == "gif")
                {
                    errorCode = "200 OK";
                    contentType = "image/gif";
                    byte[] imageBody = File.ReadAllBytes(completeFilePath);
                    contentLength = imageBody.Length;
                    sendImageResponse(errorCode, contentType, contentLength, serverInfo, date, imageBody);
                    client.Close();
                    return;
                }
                else
                {
                    errorCode = "415 Unsupported Media Type";
                    errorBody = "<!DOCTYPE html><html><body><h1>" + errorCode + "</h1></body></html>";
                    contentLength = errorBody.Length;
                    sendResponse(errorCode, contentType, contentLength, serverInfo, date, errorBody);
                    client.Close();
                    return;
                }

            }
            catch (SocketException e)
            {
                Logger.Log("[SOCKET EXCEPTION] - Error Code: " + e.ErrorCode);
            }
            catch (Exception e)
            {
                Logger.Log("[EXCEPTION] - " + e.ToString());
            }
        }

        /*
         * FUNCTION     :   sendResponse
         * DESCRIPTION  :   
         *      This takes the content for the response, formats it, and encodes it to bytes
         *      so it can be sent back to the client as a response. After the response is sent
         *      a log is made.
         * PARAMETERS   :
         *      string errorCode        :   The HTTP status code
         *      string contentType      :   The content type of the file
         *      string contentLength    :   The length of the file
         *      string server           :   Information about the server
         *      string date             :   The timestamp when the request was sent
         *      string body             :   The body of the response
         * RETURNS      :
         *      void
         */
        public static void sendResponse(string errorCode, string contentType, int contentLength, string server, string date, string body)
        {

            //Format the response string, encode it as bytes, and send it to the client socket
            try
            {
                string response = "HTTP/1.1 " + errorCode + "\r\n"
                                            + "Content-Type: " + contentType + "\r\n"
                                            + "Server: " + server + "\r\n"
                                            + "Date: " + date + "\r\n"
                                            + "Content-Length: " + contentLength + "\r\n"
                                            + "\r\n"
                                            + body;

                byte[] responseData = System.Text.Encoding.UTF8.GetBytes(response);
                client.Send(responseData);

                //Format the log depending on if an error had occured
                if(errorCode == "200 OK")
                {
                    Logger.Log($"[RESPONSE] - {contentType}, {contentLength}, {server}, {date}");
                }
                else
                {
                    Logger.Log($"[RESPONSE] - {errorCode}");
                }
            }
            catch (SocketException e)
            {
                Logger.Log("[SOCKET EXCEPTION] - Error Code: " + e.ErrorCode);
            }
            catch (Exception e)
            {
                Logger.Log("[EXCEPTION] - " + e.ToString());
            }
            
        }

        /*
         * FUNCTION     :   sendImageResponse
         * DESCRIPTION  :   
         *      This takes the content for the response, formats it, and encodes it to bytes
         *      so it can be sent back to the client as a response. After the response is sent
         *      a log is made. This function handles the body as bytes and is used for image files
         * PARAMETERS   :
         *      string errorCode        :   The HTTP status code
         *      string contentType      :   The content type of the file
         *      string contentLength    :   The length of the file
         *      string server           :   Information about the server
         *      string date             :   The timestamp when the request was sent
         *      byte[] body             :   The body of the response
         * RETURNS      :
         *      void
         */
        public static void sendImageResponse(string errorCode, string contentType, int contentLength, string server, string date, byte[] body)
        {
            //Format the response string, encode it as bytes, and send it to the client socket
            try
            {
                string response = "HTTP/1.1 " + errorCode + "\r\n"
                            + "Content-Type: " + contentType + "\r\n"
                            + "Server: " + server + "\r\n"
                            + "Date: " + date + "\r\n"
                            + "Content-Length: " + contentLength + "\r\n"
                            + "\r\n";

                byte[] responseData = System.Text.Encoding.UTF8.GetBytes(response);

                byte[] imageResponseData = new byte[responseData.Length + body.Length];
                Buffer.BlockCopy(responseData, 0, imageResponseData, 0, responseData.Length);
                Buffer.BlockCopy(body, 0, imageResponseData, responseData.Length, body.Length);

                client.Send(imageResponseData);
                Logger.Log($"[RESPONSE] - {contentType}, {contentLength}, {server}, {date}");
            }
            catch (SocketException e)
            {
                Logger.Log("[SOCKET EXCEPTION] - Error Code: " + e.ErrorCode);
            }
            catch (Exception e)
            {
                Logger.Log("[EXCEPTION] - " + e.ToString());
            }
        }
    }
}
