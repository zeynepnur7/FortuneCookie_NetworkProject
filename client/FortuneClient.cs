using System;
using System.IO; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using System.Threading.Tasks;

class FortuneClient
{
    static List<string> my_fortune_history = new List<string>();
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("--- Fortune Cookie Client is Launching.. ---");

        // start UDP listener in the background
        Task.Run(() => ListenForUdp());

        // start the main TCP connection and retry loop
        ConnectToServerWithRetry();
    }

    static void ConnectToServerWithRetry()
    {
        string serverIp = "127.0.0.1";
        int port = 5000;
         //even if the server closed, with this loop client tries again to connect
        while (true)
        {
            try
            {
                Console.WriteLine($"\n[TCP] Connecting to server {serverIp} : {port}...");
                
                using (TcpClient client = new TcpClient(serverIp, port))
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    Console.WriteLine("✅ Connected to the server successfully.");

                    //read task
                    Task.Run(() => {
                        try {
                            string? serverMsg;
                            while ((serverMsg = reader.ReadLine()) != null) {
                                if(serverMsg.StartsWith("Fortune: ")){
                                    lock(my_fortune_history){
                                        my_fortune_history.Add($"{DateTime.Now.ToShortTimeString()}-{serverMsg}");
                                    }
                                }
                                if (serverMsg.Contains("LEGENDARY")) Console.ForegroundColor = ConsoleColor.Yellow;
                                else if (serverMsg.Contains("RARE")) Console.ForegroundColor = ConsoleColor.Cyan;
                                else if (serverMsg.Contains("MESSAGE FROM ID")) Console.ForegroundColor = ConsoleColor.Magenta; 

                                Console.WriteLine("\n[SERVER]: " + serverMsg);
                                Console.ResetColor();//back to the original color
                                Console.Write("> "); //waiting for a command
                            }
                        } catch { }
                    });

                    
                    while (true)
                    {
                        Console.Write("> ");
                        string? input = Console.ReadLine();
                        if (string.IsNullOrEmpty(input)) continue;

                        string cmdUpper = input.ToUpper().Trim();
                        if(cmdUpper == "MY_FORTUNE_HISTORY"){
                            //show the history
                            Console.WriteLine("\n 📜 --- YOUR FORTUNE HISTORY ---");
                            lock(my_fortune_history){
                                if(my_fortune_history.Count==0){
                                    Console.WriteLine("You haven't received any fortunes yet!");}
                                else{
                                    foreach(var fortune in my_fortune_history){
                                        Console.WriteLine(fortune);
                                    }
                                }
                            }
                            Console.WriteLine("-----------------------------------\n");
                        }

                        else if (cmdUpper == "UPLOAD")
                        {
                            string fileToSend = @"C:\Users\ABDULLAH ERTEN\Desktop\fortuneCookie\client\new_fortunes.txt";
                            if (File.Exists(fileToSend))
                            {
                                writer.WriteLine("UPLOAD");//notificate the server to be ready
                                byte[] fileBytes = File.ReadAllBytes(fileToSend);
                                byte[] sizeBytes = BitConverter.GetBytes(fileBytes.Length);  
                                //send the size first(4byte)
                                stream.Write(sizeBytes, 0, 4);
                                //then send the file contents
                                stream.Write(fileBytes, 0, fileBytes.Length);
                                stream.Flush(); //make sure that file is sent immediatly 
                                Console.WriteLine("🚀 File is sending....");
                                //read the confirmation message from server
                            }
                            else
                            {
                                Console.WriteLine("❌ Error: File not found!!");
                            }
                        }
                        
                        else
                        {
                            //we directly send the command ,which come from user,to the server
                            writer.WriteLine(input);
                        }
                        //if user types EXIT then shut down the
                        if(cmdUpper == "EXIT") Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Connection Error: " + ex.Message);
                Console.WriteLine("🔄 Retrying in 5 seconds...");
                //wait for 5 sec and go back to the beginning of the loop
                Thread.Sleep(5000);
            }
        }
    }

    static void ListenForUdp()
    {
        int udpPort = 5001; 
        using (UdpClient udpClient = new UdpClient())
        {
            try
            {   //This allows multiple clients on the 
                // same computer to share the same port (5001).
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
                //join the multicast group
                udpClient.JoinMulticastGroup(IPAddress.Parse("239.0.0.1"));
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
               
                while (true)
                {
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine("\n📢 [BROADCAST]: " + message);
                    Console.Write("> ");
                }
            }
            catch (Exception ex) { Console.WriteLine("UDP error: " + ex.Message); }
        }
    }
}