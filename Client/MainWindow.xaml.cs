/*
* ProjectName:  MainWindow.xaml.cs
* Programer:    Dong Qian (6573448) and Monira Sultana (7308182)
* Date:         Nov 16, 2016
* Description:  This Application is a client which can send messages to another client throuth the sever
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.IO.Pipes;
using System.Threading;


namespace Client
{    
    public partial class MainWindow : Window
    {
        // Call back delegate used to update message into listbox in threads
        public delegate void MyCallback(object sender);
        public delegate void MyCallbackButtons();

        // A list to contain the NamedPipe
        List<NamedPipeClientStream> clientList = new List<NamedPipeClientStream>();

        // The BlowFish object that will encrypt and decrypt the chat messages
        BlowFish blowFish = null;

        // Bool value to check the connect status
        volatile bool connect = false;

        public MainWindow()
        {
            InitializeComponent();
            Disconnect.IsEnabled = false;
            Send.IsEnabled = false;
            Chat.IsEnabled = false;           
            Error.Content = ">>Tips: Boardcast==> Leave the \"To\" filed blank<<";

            // Create a BlowFish object with a hex key
            blowFish = new BlowFish("04B915BA43FEB5B6");

            // EXAMPLE
            //string plainText = "The quick brown fox jumped over the lazy dog.";

            //string cipherText = blowFish.Encrypt_CBC(plainText);
            //MessageBox.Show(cipherText);

            //plainText = blowFish.Decrypt_CBC(cipherText);
            //MessageBox.Show(plainText);
        }

        // When the Connection Button Clicked
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            // Check the empty textbox, indicator user for required filed
            if (You.Text != "" && Server.Text != "")
            {    
                string client1 = You.Text;
                string client2 = To.Text;
                string server = Server.Text;

                try
                {
                    // Create two NamedPipe, One for read, One for Write
                    // Wait to connect for 2 seconds, if failed, throw a exception
                    NamedPipeClientStream ClientOut = new NamedPipeClientStream(server, "ServerPipeIn", PipeDirection.InOut);
                    ClientOut.Connect(500); 
                    NamedPipeClientStream ClientIn = new NamedPipeClientStream(server, "ServerPipeOut", PipeDirection.InOut);
                    ClientIn.Connect(500);
                    // Success set the connect to true                 
                    connect = true;
                  
                    // Store the NamePipe in order to use in another event
                    clientList.Add(ClientOut);
                    clientList.Add(ClientIn);

                    // Send a message to tell the Sever who am i and who i want to talk to
                    StreamWriter output = new StreamWriter(clientList[0]);
                    string tell = client1 + "," + client2;
                    output.WriteLine(tell);
                    output.Flush();

                    // After connecting instablished, Enable the Disconnect button, Disable Connect button and required filed.
                    Disconnect.IsEnabled = true;

                    if (Disconnect.IsEnabled == true)
                    {
                        Connect.IsEnabled = false;
                        You.IsEnabled = false;
                        To.IsEnabled = false;
                        Server.IsEnabled = false;
                        Send.IsEnabled = true;
                        Chat.IsEnabled = true;
                    }

                    // Start a Thread to receive message constanly
                    Thread t1 = new Thread(new ThreadStart(Readmsg));
                    t1.Start();

                    // Notify the user Sever is connected
                    Error.Content = ">>Server is Connected<<";
                }
                // Show the error message when Server is not avaiable
                catch(Exception ex)
                {
                    Error.Content = ex.Message;
                }
                
            }
            // Required filed can not be blank
            else
            {
                Error.Content = ">>Your name and Server can not be blank<<";
            }
        }

        
        // When the Send button Clicked
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check the connection status. If the server is connected
                // Read the text form textbox and send it to the server
                if (connect)
                {
                    if (Chat.Text != "")
                    {
                        StreamWriter output = new StreamWriter(clientList[0]);
                        string x = Chat.Text;
                        History.Items.Add("[ " + DateTime.Now.ToString("HH:mm") + " ] You:  " + Chat.Text);
                        History.ScrollIntoView(History.Items[History.Items.Count - 1]);

                        string outp = Chat.Text;
                       
                        output.WriteLine(outp);
                        output.Flush();
                        Chat.Clear();
                    }
                    else
                    {
                        Error.Content = ">>!!!Message can not be blank!!!<<";
                    }
                }
                // Otherwiser notify the user Sever is not connected
                else
                {
                    Error.Content = ">>!!!You are not connected to the Server!!!<<";
                }
            }
            catch(Exception ex)
            {
                Error.Content = ex;
            }
        }
        /*
       Method :		SendCrypt_Click()
       Description:    When the Connection Button Clickedit sent the message with encrepted.
       Return value:	No
       */

        private void SendCrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check the connection status. If the server is connected
                // Read the text form textbox and send it to the server
                if (connect)
                {
                    if (Chat.Text != "")
                    {
                        StreamWriter output = new StreamWriter(clientList[0]);
                        StreamWriter cryptOutput = new StreamWriter(clientList[0]);
                        string x = Chat.Text;
                        History.Items.Add("[ " + DateTime.Now.ToString("HH:mm") + " ] You:  " + Chat.Text);
                        History.ScrollIntoView(History.Items[History.Items.Count - 1]);

                        string outp = Chat.Text;
                        output.WriteLine(outp);
                        output.Flush();
                        //Applying the BlowFish encryption to the message
                        string encrypted = blowFish.Encrypt_CBC(outp);

                        cryptOutput.WriteLine(encrypted);
                        //output.WriteLine(outp);
                        cryptOutput.Flush();
                        Chat.Clear();
                    }
                    else
                    {
                        Error.Content = ">>!!!Message can not be blank!!!<<";
                    }
                }
                // Otherwiser notify the user Sever is not connected
                else
                {
                    Error.Content = ">>!!!You are not connected to the Server!!!<<";
                }
            }
            catch (Exception ex)
            {
                Error.Content = ex;
            }

        }

        /*
        Method :		readmsg()
        Description:    This is a Thread keep reading the message which recived from the server
        Return value:	No
        */
        private void Readmsg()
        {
            try
            {
                MyCallback callback = new MyCallback(UpdateChatArea);
                StreamReader input = new StreamReader(clientList[1]);

                // Read the message and invoke the update listbox
                while (connect)
                {
                    string inp = input.ReadLine();

                    if (inp != null)
                    {
                        //==============================
                        // DECRYPT the receiving message
                        //inp = blowFish.Decrypt_CBC(inp);

                        if (inp == "shutdown")
                        {
                            // check if the client is connected
                            Shutdown();
                            break;
                        }
                        Dispatcher.Invoke(callback, inp);
                    }
                }
            }
            catch(Exception ex)
            {
                Error.Content = ex;
            }
        }


        /*
        Method :		UpdateChatArea()
        Parameter:      Object obj: The received message
        Description:    This is a a callback funtion to update the listbox
        Return value:	No
        */
        private void UpdateChatArea(Object obj)
        {
            try
            {
                string client2 = To.Text;
                string msg = (string)obj;
                // Add the message to the listbox and auto scroll tp last one
                History.Items.Add("\t\t[ " + DateTime.Now.ToString("HH:mm") + " ]  " + msg);
                History.ScrollIntoView(History.Items[History.Items.Count - 1]);
            }
            catch(Exception ex)
            {
                Error.Content = ex;
            }
        }

        // When disconnect button clicked
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            // check if the client is connected
            try
            {
                if (clientList[0].IsConnected)
                {
                    // Send "shutdown" message to Server
                    StreamWriter output = new StreamWriter(clientList[0]);
                    string shutdownString = "shutdown";
                    output.WriteLine(shutdownString);
                    output.Flush();
                    Shutdown();
                }
                else
                {
                    Error.Content = ">>!!!You are not connected to the Server!!!<<";
                }
            }
            catch(Exception ex)
            {
                Error.Content = ex;
            }
            
        }


        /*
        Method :		Shutdown()
        Description:    close the NamedPipe, disable buttons
        Return value:	No
        */
        private void Shutdown()
        {
            try
            {
                MyCallbackButtons DisableButtons = new MyCallbackButtons(closeButton);
                connect = false;
                // Close the NamedPipe
                clientList[0].Dispose();
                clientList[0].Close();
                clientList[0].Dispose();
                clientList[1].Close();
                // Clear the container
                clientList.Clear();
                Dispatcher.Invoke(DisableButtons);
            }
            catch(Exception ex)
            {
                Error.Content = ex;
            }          
        }


        /*
       Method :		    closeButton()
       Description:     Callback function to close button
       Return value:	No
       */
        private void closeButton()
        {
            // Enable buttons for connection
            Connect.IsEnabled = true;
            You.IsEnabled = true;
            To.IsEnabled = true;
            Server.IsEnabled = true;
            Disconnect.IsEnabled = false;
            Chat.IsEnabled = false;
            Send.IsEnabled = false;
            //History.Items.Clear();
            Error.Content = "The Server is down";
        }

        
    }
}
