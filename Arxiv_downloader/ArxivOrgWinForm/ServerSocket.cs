using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArxivOrgWinForm
{


    public class ServerSocket
    {
        private TcpClient client;
        private Thread readWriteThread;
        private NetworkStream networkStream;        

        public ServerSocket(string ip, int port)
        {
            try
            {
                client = new TcpClient(ip, port);                
            }
            catch (SocketException)
            {
                throw new Exception("Error connection modem!");
            }

            //Assign networkstream
            networkStream = client.GetStream();

            //start socket read/write thread
            readWriteThread = new Thread(readWrite);
            readWriteThread.Start();
        }

        private void readWrite()
        {
            string command = "", recieved = "";

            //Read first thing givent o us
            recieved = read();            
            Thread.Sleep(8000);
            //Console.WriteLine(recieved);

            //Set up connection loop
            for (int i = 1; i <=3; i++)
            {
                //if (i == 0) command = "Telnet 192.168.1.1";
                if (i == 1) command = "admin";
                if (i == 2) command = "admin";
                if (i == 3) command = "reboot";

                //if (command == "exit")
                //    break;

                write(command);
                Thread.Sleep(8000);

                //recieved = read();

            }

            //Disconnected from server
            networkStream.Close();
            client.Close();
        }

        public void write(string message)
        {
            message += Environment.NewLine;
            //byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            networkStream.Write(messageBytes, 0, messageBytes.Length);            
            //networkStream.Write(Encoding.ASCII.GetBytes(message), 0, message.Length);
            networkStream.Flush();
        }

        public string read()
        {
            string recieved = "";

            try
            {
                byte[] data = new byte[1024];

                bool canRead = networkStream.CanRead;
                int size = networkStream.Read(data, 0, data.Length);
                recieved = Encoding.ASCII.GetString(data, 0, size);                
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
            
            return recieved;
        }
    }
}
