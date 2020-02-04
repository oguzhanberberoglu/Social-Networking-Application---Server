using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security;
namespace server
{
    public partial class Form1 : Form
    {
        // static string filePath = @"C:\Users\asus\Desktop\user_db.txt";
        static string filePath = @"C:\Users\asus\Downloads\user_db.txt";
        string[] ClientList = System.IO.File.ReadAllLines(filePath);
        List<string> added = new List<string>();
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();
        string[,] vec = new string[300, 300];
        string[,] vec_current = new string[300, 300];
        string[,] notifications = new string[300, 300];
        string[,] private_message = new string[300, 10000];
        bool check_name = true;
        //string[] NameList=new string[300];

        bool terminating = false;
        bool listening = false;

        string nick = "";
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;
            for (int i = 0; i < 300; i++)
            {
                vec[i, 0] = ClientList[i];
                for (int j = 1; j < 300; j++)
                {
                    vec[i, j] = "1";
                }
            }
            for (int i = 0; i < 300; i++)
            {
                vec_current[i, 0] = ClientList[i];
                for (int j = 1; j < 300; j++)
                {
                    vec_current[i, j] = "1";
                }

            }
            for (int i = 0; i < 300; i++)
            {
                notifications[i, 0] = ClientList[i];
                for (int j = 1; j < 300; j++)
                {
                    notifications[i, j] = "1";
                }

            }
            for (int i = 0; i < 300; i++)
            {
                private_message[i, 0] = ClientList[i];
                for (int j = 1; j < 10000; j++)
                {
                    private_message[i, j] = "1";
                }

            }
            if (Int32.TryParse(textBox_port.Text, out serverPort) && serverPort <= 65535 && serverPort > 0)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(100);

                listening = true;
                button_listen.Enabled = false;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    Byte[] buffer = new Byte[128];
                    newClient.Receive(buffer);
                    nick = Encoding.ASCII.GetString(buffer);   // Encodes the name of the client and eliminates the
                    nick = nick.Substring(0, nick.IndexOf("\0"));
                    int count = 0;
                    bool checked_item = true;
                    foreach (string item in ClientList)
                    {
                        if (item == nick)
                        {
                            //Socket thisClient = clientSockets[clientSockets.Count() - 1];
                            if (!clientSockets.Contains(newClient) && !added.Contains(nick))
                            {
                                //Socket newClient = serverSocket.Accept();
                                count++;
                                clientSockets.Add(newClient);
                                added.Add(nick);
                                logs.AppendText(nick + " has connected to server\n");
                                buffer = Encoding.Default.GetBytes(nick + " has connected to server\n");
                                newClient.Send(buffer);
                                Thread receiveThread = new Thread(Receive);
                                receiveThread.Start();
                            }
                            int i;
                            bool notifi = true;
                            //bool free_curr = true;
                            for (i = 0; i < 300 && notifi != false; i++)//when client reconnected send notifications
                            {
                                if (notifications[i, 0] == nick)
                                {
                                    for (int x = 1; x < 300 && notifi; x++)
                                    {
                                        if (notifications[i, x] == "1")
                                            notifi = false;
                                        if (notifi)
                                        {
                                            buffer = Encoding.Default.GetBytes("Notification: \n" + notifications[i, x] + "\n");
                                            logs.AppendText("Notification: \n" + notifications[i, x] + "\n");
                                            newClient.Send(buffer);
                                            notifications[i, x] = "1";
                                        }
                                        //else if (notifi && !free_curr)
                                        //{
                                        //    buffer = Encoding.Default.GetBytes("Notification: \n" + notifications[i, x] + "\n");
                                        //    newClient.Send(buffer);
                                        //}
                                    }
                                }
                            }
                            notifi = true;
                            bool outof = true;
                            for (int w = 0; w < 300 && outof; w++)//when client reconnected send private messages
                            {
                                if (private_message[w, 0] == nick)
                                {
                                    for (int y = 1; y < 10000 && outof; y++)
                                    {
                                        if (private_message[w, y] == "1")
                                            outof = false;
                                        if (outof)
                                        {
                                            buffer = Encoding.Default.GetBytes(private_message[w, y] + "\n");
                                            newClient.Send(buffer);
                                            logs.AppendText(private_message[w, y] + "\n");
                                            private_message[w, y] = "1";
                                        }

                                    }
                                }
                            }
                            checked_item = false;
                        }
                        if (checked_item == false)
                            break;
                    }

                    if (count == 0)
                    {
                        logs.AppendText(nick + " can not connected to server\n");
                        buffer = Encoding.Default.GetBytes(nick + " can not connected to server\n");
                        newClient.Send(buffer);
                        newClient.Dispose();
                        newClient.Disconnect(false);
                        //newClient.Shutdown();
                        newClient.Close();
                        //clientSockets.Remove(newClient);
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }
        private void Receive()
        {
            Socket thisClient = clientSockets[clientSockets.Count() - 1];
            bool connected = true;

            while (connected && !terminating)
            {
                try
                {
                    Byte[] buffer = new Byte[128];
                    thisClient.Receive(buffer);
                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    string[] split = incomingMessage.Split(' ');

                    if (incomingMessage != "" && split[3] == "invitiation")//to send invitation list
                    {
                        int i;
                        bool invite = true;
                        for (i = 0; i < 300 && invite != false; i++)
                        {
                            if (vec[i, 0] == split[0] + " " + split[1])
                            {
                                for (int x = 1; x < 300; x++)
                                {
                                    if (vec[i, x] == "1")
                                        invite = false;
                                    if (invite)
                                    {
                                        buffer = Encoding.Default.GetBytes("invitiation" + " " + vec[i, x] + "\n");
                                        logs.AppendText("One of the invitations for " + split[0] + " " + split[1] + " is " + vec[i, x] + "\n");
                                        thisClient.Send(buffer);
                                    }
                                }
                                invite = true;
                            }
                        }
                    }
                    else if (incomingMessage != "" && split[3] == "current")//to send current friends
                    {
                        int i;
                        bool cur = true;
                        for (i = 0; i < 300 && cur != false; i++)
                        {
                            if (vec_current[i, 0] == split[0] + " " + split[1])
                            {
                                for (int x = 1; x < 300; x++)
                                {
                                    if (vec_current[i, x] == "1")
                                        cur = false;
                                    if (cur)
                                    {
                                        buffer = Encoding.Default.GetBytes("current" + " " + vec_current[i, x] + "\n");
                                        logs.AppendText("One of the current friends for " + split[0] + " " + split[1] + " is " + vec_current[i, x] + "\n");
                                        thisClient.Send(buffer);
                                    }
                                }
                            }
                        }
                        cur = true;
                    }
                    else if (incomingMessage != "" && split[3] == "befriend")// ad all requests of being friends to vec
                    {
                        bool anyinvitation = true;
                        if (split[4] + " " + split[5] != split[0] + " " + split[1])
                        {
                            for (int m = 0; m < 300 && anyinvitation; m++)//is there any invitiation in sended cleint
                            {
                                if (vec[m, 0] == split[0] + " " + split[1])
                                {
                                    for (int k = 1; k < 300 && anyinvitation; k++)
                                    {
                                        if (vec[m, k] == split[4] + " " + split[5])
                                            anyinvitation = false;
                                    }
                                }
                            }
                            int i;
                            for (i = 0; i < 300 && check_name != false; i++)
                            {
                                if (vec[i, 0] == split[4] + " " + split[5])
                                {
                                    for (int x = 1; x < 300; x++)//check is there any friendship among them
                                    {
                                        if (vec_current[i, x] == split[0] + " " + split[1])
                                            check_name = false;
                                    }

                                    if (check_name != false && anyinvitation)
                                    {
                                        check_name = false;
                                        bool loop = true;
                                        for (int a = 0; a < 300 && loop; a++)//add invitaiton to second client
                                        {
                                            if (vec[i, a] == "1")
                                            {
                                                loop = false;
                                                vec[i, a] = split[0] + " " + split[1];
                                                logs.AppendText(split[0] + " wanted to be friend of " + vec[i, 0] + " " + "\n");
                                            }
                                        }

                                    }
                                    else if (!check_name && anyinvitation)
                                    {
                                        buffer = Encoding.Default.GetBytes(split[4] + " " + split[5] + " is already your friend" + "\n");
                                        thisClient.Send(buffer);
                                    }
                                }
                            }
                        }
                        check_name = true;
                    }
                    else if (incomingMessage != "" && split[3] == "Accept")//add friendships to corresponding names to vec_current
                    {
                        int i;
                        bool accept = true;
                        bool isalready = true;
                        
                        for (i = 0; i < 300 && accept != false; i++)
                        {
                            if (vec_current[i, 0] == split[0] + " " + split[1])
                            {
                                for (int x = 1; x < 300 && isalready; x++)//check is there any friendship among them
                                {
                                    if (vec_current[i, x] == split[4] + " " + split[5])
                                        isalready = false;
                                }
                                accept = false;
                                bool accept_cur = true;
                                for (int a = 1; a < 300 && accept_cur && isalready; a++)//add friend to first client
                                {
                                    if (vec_current[i, a] == "1")
                                    {
                                        accept_cur = false;
                                        vec_current[i, a] = split[4] + " " + split[5];
                                    }
                                }
                                bool out_loop = true;
                                for (int x = 1; x < 300 && out_loop && isalready; x++)//delete invitation 
                                {
                                    if (vec[i, x] == split[4] + " " + split[5])
                                    {
                                        vec[i, x] = "1";
                                        out_loop = false;
                                    }
                                }
                            }
                        }
                        isalready = true;
                        accept = true;
                        for (i = 0; i < 300 && accept != false; i++)
                        {
                            string aad = split[4] + " " + split[5];
                            if (vec_current[i, 0] == aad)
                            {
                                for (int x = 1; x < 300 && isalready; x++)//check is there any friendship among them
                                {
                                    if (vec_current[i, x] == split[0] + " " + split[1])
                                        isalready = false;
                                }
                                accept = false;
                                bool accept_cur = true;
                                for (int a = 1; a < 300 && accept_cur && isalready; a++)//add friend to second client
                                {
                                    if (vec_current[i, a] == "1")
                                    {
                                        accept_cur = false;
                                        vec_current[i, a] = split[0] + " " + split[1];
                                        logs.AppendText(vec_current[i, a] + " accepted friendship invitation of " + aad + " " + "\n");
                                    }
                                }
                                bool out_loop = true;
                                for (int x = 1; x < 300 && out_loop && isalready; x++)//delete invitation 
                                {
                                    if (vec[i, x] == split[0] + " " + split[1])
                                    {
                                        vec[i, x] = "1";
                                        out_loop = false;
                                    }
                                }
                                bool cur_not = true;
                                for (int a = 1; a < 300 && cur_not && isalready; a++)
                                {
                                    if (notifications[i, a] == "1")//add notification to second client
                                    {
                                        cur_not = false;
                                        notifications[i, a] = split[0] + " " + split[1] + " accepted your friendship invitation ";
                                        
                                    }
                                    try
                                    {
                                        bool send = true;
                                        for (int e = 0; e < 300 && !cur_not&&send; e++)
                                        {
                                            if (e < added.Count)
                                            {
                                                if (added[e] == vec_current[i, 0])//send this notification to second client
                                                {
                                                    buffer = Encoding.Default.GetBytes("Notification: \n" + notifications[i, a] + "\n");
                                                    if (clientSockets[e].Connected == true)
                                                    {
                                                        clientSockets[e].Send(buffer);
                                                        notifications[i, a] = "1";
                                                    }
                                                    send = false;
                                                }
                                            }
                                            else
                                                send = false;
                                        }
                                    }
                                    catch
                                    {
                                        logs.AppendText("There is a problem! Check the connection...");
                                        terminating = true;
                                        serverSocket.Close();
                                    }
                                }
                               
                             
                            }
                        }
                   
                    }
                    else if (incomingMessage != "" && split[3] == "Reject")//delete rejected requests of being friend from vec
                    {
                        int i;
                        bool reject = true;
                        reject = true;
                        for (i = 0; i < 300 && reject != false; i++)//delete invitation from first client
                        {
                            if (vec[i, 0] == split[0] + " " + split[1])
                            {

                                for (int x = 1; x < 300 && reject; x++)
                                {
                                    if (vec[i, x] == split[4] + " " + split[5])
                                    {
                                        vec[i, x] = "1";
                                        reject = false;
                                        logs.AppendText(split[0] + " " + split[1] + " rejected friendship invitation of " + split[4] + " " + split[5] + " " + "\n");

                                    }
                                }
                                reject = false;
                            }
                        }
                        reject = true;
                        for (i = 0; i < 300 && reject != false; i++)
                        {
                            if (vec[i, 0] == split[4] + " " + split[5])
                            {

                                for (int x = 1; x < 300 && reject; x++)//delete invitation from second client
                                {
                                    if (vec[i, x] == split[0] + " " + split[1])
                                    {
                                        vec[i, x] = "1";
                                        reject = false;
                                        logs.AppendText(split[0] + " " + split[1] + " rejected friendship invitation of " + split[4] + " " + split[5] + " " + "\n");
                                    }
                                }
                                bool cur_not = true;
                                for (int a = 1; a < 300 && cur_not; a++)//add notification about acceptance
                                {
                                    if (notifications[i, a] == "1")
                                    {
                                        cur_not = false;
                                        notifications[i, a] = split[0] + " " + split[1] + " rejected your friendship invitation ";
                                    }
                                    try
                                    {
                                        bool send = true;
                                        for (int e = 0; e < 300&&send&&!cur_not; e++)//send this notification to second client
                                        {
                                            if (e < added.Count)
                                            {
                                                if (added[e] == vec[i, 0])
                                                {
                                                    buffer = Encoding.Default.GetBytes("Notification: \n" + notifications[i, a] + "\n");
                                                    if (clientSockets[e].Connected)
                                                    {
                                                        clientSockets[e].Send(buffer);
                                                        notifications[i, a] = "1";
                                                    }
                                                    send = false;
                                                }
                                            }
                                            else
                                                send = false;
                                        }
                                    }
                                    catch
                                    {
                                        logs.AppendText("There is a problem! Check the connection...");
                                        terminating = true;
                                        serverSocket.Close();
                                    }
                                }
                                reject = false;
                            }
                        }

                  
                    }
                    else if (incomingMessage != "" && split[3] == "private")
                    {
                        string message = "Private with ";
                        message += split[0] + " " + split[1] + ": \n";
                        int x_cord = 0, ycord = 0;
                        for (int i = 6; i < split.Length; i++)//create message
                        {
                            message += split[i] + " ";
                        }
                        bool is_current = true;
                        for (int m = 0; m < 300 && is_current; m++)//get position of second client in current friends
                        {
                            if (vec_current[m, 0] == split[0] + " " + split[1])
                            {
                                for (int k = 1; k < 300 && is_current; k++)
                                {
                                    if (vec_current[m, k] == split[4] + " " + split[5])
                                    {
                                        is_current = false;
                                        x_cord = m;
                                        ycord = k;
                                    }
                                }
                            }
                        }
                        if (is_current == false)
                        {
                            bool enough = true;
                            int k;
                            for (int d = 0; d < 300 && enough; d++)
                            {
                                if (private_message[d, 0] == vec_current[x_cord, ycord])
                                {
                                    for ( k = 1; k < 10000&&enough; k++)//add message to private message array
                                    {
                                        if(private_message[d,k]=="1")
                                        {
                                            private_message[d, k] = message;
                                            logs.AppendText(message);
                                            enough = false;
                                        }
                                    }
                                    try
                                    {
                                        bool remove = true;
                                        for (int e = 0; e < 300&&!enough&&remove; e++)
                                        {
                                            if (e < added.Count)
                                            {
                                                if (added[e] == vec_current[x_cord, ycord])//send this message to second client
                                                {
                                                    buffer = Encoding.Default.GetBytes("Notification: \n" + message + "\n");
                                                    if (clientSockets[e].Connected)
                                                    {
                                                        clientSockets[e].Send(buffer);
                                                        private_message[d, k] = "1";
                                                    }
                                                    remove = false;
                                                }
                                            }
                                            else
                                                remove = false;
                                        }
                                    }
                                    catch 
                                    {
                                        logs.AppendText("There is a problem! Check the connection...");
                                        terminating = true;
                                        serverSocket.Close();
                                    }
                                }
                            }

                        }
                    }
                    else if (incomingMessage != "" && split[3] == "Remove")
                    {
                        bool done = true;
                        for (int i = 0; i < 300 && done; i++)//remove friend list of first client
                        {
                            if (vec_current[i, 0] == split[0] + " " + split[1])
                            {
                                for (int j = 1; j < 300 && done; j++)
                                {
                                    if (vec_current[i, j] == split[4] + " " + split[5])
                                    {
                                        vec_current[i, j] = "1";
                                        done = false;
                                    }
                                }
                            }
                        }
                        done = true;
                        for (int i = 0; i < 300 && done; i++)
                        {
                            if (vec_current[i, 0] == split[4] + " " + split[5])
                            {
                                for (int j = 1; j < 300 && done; j++)//remove friend list of second client
                                {
                                    if (vec_current[i, j] == split[0] + " " + split[1])
                                    {
                                        vec_current[i, j] = "1";
                                        done = false;
                                    }
                                }
                                bool notif = true;
                                for (int a = 1; a < 300 && notif; a++)
                                {
                                    if (notifications[i, a] == "1")//add notification to the notification list of second client
                                    {
                                        notif = false;
                                        notifications[i, a] = split[0] + " " + split[1] + " removed you from its current friends ";
                                        logs.AppendText(split[0] + " " + split[1] + " removed " + split[4] + " " + split[5] + " " + "from its current friends ");
                                    }
                                    try
                                    {
                                        bool reject = true;
                                        for (int e = 0; e < 300 && !notif&&reject; e++)
                                        {
                                            if (e < added.Count)
                                            {
                                                if (added[e] == vec_current[i, 0])//send this notification to second client
                                                {
                                                    buffer = Encoding.Default.GetBytes("Notification: \n" + notifications[i, a] + "\n");
                                                    if (clientSockets[e].Connected)
                                                    {
                                                        clientSockets[e].Send(buffer);
                                                        notifications[i, a] = "1";
                                                    }
                                                    reject = false;
                                                }
                                            }
                                            else
                                                reject = false;
                                        }
                                    }
                                    catch 
                                    {
                                        logs.AppendText("There is a problem! Check the connection...");
                                        terminating = true;
                                        serverSocket.Close();
                                    }
                                   
                                }
                            }
                        }
                        done = true;
                    }
                    else
                    {
                        logs.AppendText(incomingMessage + "\n");
                        foreach (Socket client in clientSockets) // Let all clients know the round number.
                        {
                            try
                            {
                                if (incomingMessage != "")
                                {
                                    buffer = Encoding.Default.GetBytes("General \n" + incomingMessage + "\n");
                                    if (client != thisClient)
                                    {
                                        client.Send(buffer);
                                    }
                                }
                            }

                            catch
                            {
                                logs.AppendText("There is a problem! Check the connection...");
                                terminating = true;
                                serverSocket.Close();
                            }
                        }
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        Byte[] buffer = new Byte[128];
                        int index = clientSockets.IndexOf(thisClient);
                        clientSockets.Remove(thisClient);
                        logs.AppendText(added[index] + " client has disconnected\n");
                        buffer = Encoding.Default.GetBytes(added[index] + " client has disconnected\n");
                        foreach (Socket client in clientSockets)
                        {
                            if (client != thisClient)
                            {
                                client.Send(buffer);
                            }
                        }
                        added.RemoveAt(index);
                    }
                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;
                }
            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            logs.AppendText("Server has been terminated");
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }


    }
}
