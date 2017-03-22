/*
* ProjectName:  Server.cs
* Programer:    Dong Qian (6573448) and Monira Sultana (7308182)
* Date:         Nov 16, 2016
* Description:  This Application is a server to receive the message from one client and send the message to another client.
*               It can accept mutiple clients.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections;

namespace IPCNamedPipeServer
{
    class server
    {
        // Container to store the NamePipe instants
        public static Dictionary<string, Pipe> clientDT = new Dictionary<string, Pipe>();
        // Struct to store two pipes, one for read, one for write
        public struct Pipe
        {
           public NamedPipeServerStream serverIn;
           public NamedPipeServerStream serverOut;
        }

        // bool vaule to indicator the server is down
        public static volatile bool shutdown = false;

        public static void Main(string[] args)
        {
            Console.WriteLine("\t\t\t\t>>Welcome to the SuperChat Server<<\n\n");
            Thread start = new Thread(new ThreadStart(StartServer));
            start.Start();
            Console.WriteLine(">>>>The Server is running<<<<\n\n");

            // shutdown
            Console.WriteLine("\nEnter \"q\" to Stop Server\n");
            char down;
            do
            {
                down = Console.ReadKey().KeyChar;
                if (down == 'q')
                {
                    shutdown = true;
                    Disconnect();
                    break;
                }
                Console.Write("\nPlease enter \"q\" to Exit\n");
            } while (down != 'q');

            Environment.Exit(0);
        }


        /*
       Method :		   StartServer()
       Description:    This is a Thread keep listen for the new connection
       Return value:	No
       */
        public static void StartServer()
        {
            try
            {
                // Main thread to listen for connection
                while (!shutdown)
                {
                    // Two pipes, one for read, one for write
                    NamedPipeServerStream serverIn = new NamedPipeServerStream("ServerPipeIn", PipeDirection.InOut, 254);
                    NamedPipeServerStream serverOut = new NamedPipeServerStream("ServerPipeOut", PipeDirection.InOut, 254);
                    // Both waiting for connection
                    serverIn.WaitForConnection();
                    serverOut.WaitForConnection();

                    // Store the instants of the NamedPipe into struct 
                    Pipe newPipe = new Pipe();
                    newPipe.serverIn = serverIn;
                    newPipe.serverOut = serverOut;


                    // Read the client message 
                    StreamReader input = new StreamReader(serverIn);
                    String inp = input.ReadLine();

                    // Get the message from the client ( who send the message, where needs to be send)
                    char[] del = { ',' };
                    // Who send message
                    string come = inp.Split(',')[0];
                    // Where it send to
                    string to = inp.Split(',')[1];

                    // Add client to the container to keep track
                    if (!clientDT.ContainsKey(come))
                    {
                        clientDT.Add(come, newPipe);
                        //Console.WriteLine(">>" + come + " is connected to Server<<\n");
                    }
                    // start the thread for mutiple clients
                    Thread t = new Thread(() => ServerThread(come, to));
                    t.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("NamePipe Failed >> " + ex);
            }        
        }


        /*
        Method :		Disconnect()
        Description:    Disconnect all NamedPipe and send message to all clients notify them server is down
        Return value:	No
        */
        public static void Disconnect()
        {
            try
            {
                // Send message to all clients
                foreach (var index in clientDT)
                {
                    StreamWriter output = new StreamWriter(index.Value.serverOut);
                    output.WriteLine("shutdown");
                    output.Flush();
                }

                Thread.Sleep(1000);

                // Close all NamedPipe
                foreach (var index in clientDT)
                {
                    //Console.WriteLine("\n" + index.Key + " is disconnected");
                    index.Value.serverIn.Dispose();
                    index.Value.serverOut.Dispose();
                    index.Value.serverIn.Close();
                    index.Value.serverOut.Close();                   
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }



        /*
        Method :		ServerThread()
        Description:    This is a Thread keep runing to recievd message and forward the message for each instants of NamedPipe
        Parameter:		String from:    Who sends the message
                        String to:      where it sends to
        Return value:	No
        */
        public static void ServerThread(string from, string to)
        {
            // This Thread keep running until it receive "shutdown" message
            while (!shutdown)
            {
                try
                {
                    // Read the message
                    StreamReader input = new StreamReader(clientDT[from].serverIn);
                    String inp = input.ReadLine();
                    if (inp != null)
                    {
                        // If the message is ""shutdown", close this instant of this NamePipe. 
                        // Remove this client from the container
                        // Close the thread
                        if (inp == "shutdown")
                        {
                            clientDT[from].serverIn.Close();
                            clientDT[from].serverOut.Dispose();
                            clientDT.Remove(from);
                            //Console.WriteLine(">>" + from + " is disconnected<<\n");
                            break;
                        }
                        // if the "To" client is not identified, It means needs to broadcast the message to all clients
                        else if (to == "")
                        {
                            foreach (var index in clientDT)
                            {
                                // Not include itself
                                if (index.Value.serverIn != clientDT[from].serverIn)
                                {
                                    StreamWriter output = new StreamWriter(index.Value.serverOut);
                                    output.WriteLine(from + ": " + inp);
                                    output.Flush();
                                }
                            }
                            //Console.WriteLine(from + ": Broadcast to all clients ==> " + inp);
                        }
                        // Else if the "To" is connected and specificed, forward the message to it
                        else if (clientDT.ContainsKey(to))
                        {
                            StreamWriter output = new StreamWriter(clientDT[to].serverOut);
                            //Console.WriteLine(from + " Sends == >\"" + inp + "\" to " + to);

                            // "austin"
                            // ":    "
                            // "fdas5#@%@" -- our original message

                            // "austin :   fdas5#@%@" -- modified the message! not what we want
                            output.WriteLine(from + ":  " + inp);
                            output.Flush();
                        }
                        // If the "To" is not connected, send back to client indicator "To" is not connected
                        else
                        {
                            StreamWriter output = new StreamWriter(clientDT[from].serverOut);
                            output.WriteLine(to + " is not connected");
                            output.Flush();
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}


