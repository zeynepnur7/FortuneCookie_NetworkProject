using System.IO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using FortuneCookie.Common;

class Server{
    static Dictionary<int, StreamWriter> clientMap = new Dictionary<int, StreamWriter>();
    static int idCounter = 1;
    //list for fortunes
    static List<Fortune> fortunePool = new List<Fortune>();
    static Random rnd = new Random();
    
    static void Main(string[] args)
    {
    Console.OutputEncoding = System.Text.Encoding.UTF8;    
        // load fortunes from the file
        fortunePool = LoadFortunes();
        Console.WriteLine($"{fortunePool.Count} fortunes loaded");
         int port = 5000; 


    //start the server
    try{
        TcpListener listener = new TcpListener(IPAddress.Any, port);
    listener.Start(); //start listening for incoming connections
    //start the UDP broadcast
    Task.Run(() => {
        while(true) {
            BroadcastDailyFortune();
            System.Threading.Thread.Sleep(60000); // release every minute 
        }
    });
    Console.WriteLine("Fortune Bank Server has started..");

    while (true){
        try{
            TcpClient client = listener.AcceptTcpClient();//accept incoming connection
            Console.WriteLine("A client has connected");
            Task.Run(() => HandleClient(client)); // run every client in a seperate thread
        }
        catch (Exception ex){
                // while client connecting if there is an error,  server shouldn't crash out. 
            // wait for the next client to connect
                Console.WriteLine("Connection acceptance error: " + ex.Message);
            }
        }
    }
    catch (Exception ex){
        // if server failed to start
        Console.WriteLine("Server failed to start: " + ex.Message);}
        } //end of main

    static List<Fortune> LoadFortunes(){ //loading fortunes from txt file
        var pool = new List<Fortune>();
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fortunes.txt");
            if (File.Exists(path))
            {   
                foreach(var line in File.ReadAllLines(path)){
                    var parts = line.Split('|');
                    if(parts.Length == 3){
                        pool.Add(new Fortune{
                            Category = parts[0].Trim(),
                            Rarity = parts[1].Trim(),
                            Text = parts[2].Trim()
                        });
                    }
                }
            }
        }
        catch (Exception ex) { 
            Console.WriteLine("File Read Error: " + ex.Message); }

        // if file doesn't exist or there is an error, return a default fortune
        if(pool.Count == 0) pool.Add(new Fortune { Category ="General", Rarity ="Common", Text="Better luck next time!"});
        return pool;
    }


    static void HandleClient(TcpClient client)//this method will be run on distinct thread for each client
    {   
        int myId;
        //safe way to read and write to the network stream
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream))
        using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
        {
            lock(clientMap){
                myId = idCounter++;
                clientMap.Add(myId, writer);
            }
            try
            {
                // send welcome message to the client
                writer.WriteLine($"--- Welcome to the Fortune Cookie Network! Your ID is: {myId} ---");
                writer.WriteLine("Type 'GET_FORTUNE' to see your future, 'SEND <ID> <msg>' to send a fortune to a client, 'UPLOAD' to upload a new fortune file to fortune pool or 'EXIT' to quit.");
                

                string? request;
                // read messages from the client until the connection is closed
                while ((request = reader.ReadLine()) != null)
                {   
                    string upperRequest = request.ToUpper();
                    //send
                    if(upperRequest.StartsWith("SEND")){
                        string[] parts = request.Split(' ', 3);
                        if(parts.Length >= 3 && int.TryParse(parts[1], out int targetId)){
                            lock (clientMap) {
                            if (clientMap.TryGetValue(targetId, out StreamWriter targetWriter)) {
                                targetWriter.WriteLine($"\n📩 [MESSAGE FROM ID {myId}]: {parts[2]}");
                                writer.WriteLine($"SUCCESS: Message delivered to ID {targetId}.");
                            }
                            else writer.WriteLine("ERROR: Target ID not found.");
                            }
                        }
                        else writer.WriteLine("ERROR: Use format 'SEND <ID> <Message>'");
                    }
                    else if (upperRequest.StartsWith("GET_FORTUNE") )
                    {
                        //deciding category
                        string[] parts = request.Split(' ');
                        string requestedCategory = parts.Length > 1 ? parts[1] : null;
                        // rarity logic 
                        int roll = rnd.Next(1,101);
                        string selectedRarity;
                        if (roll <= 70) selectedRarity ="Common"; // %70 probability
                        else if (roll <= 95) selectedRarity = "Rare"; // %25 probability
                        else selectedRarity = "Legendary"; // %5 probability
                        //Filter by rarity and (if asked) by category
                        var candidates = fortunePool.FindAll(f=>
                        f.Rarity.Equals(selectedRarity, StringComparison.OrdinalIgnoreCase)&&
                        (requestedCategory == null || f.Category.Equals(requestedCategory,StringComparison.OrdinalIgnoreCase)));

                        //Fallback: If there is no fallback for the search criteria, remove the rarity filter and look only at the category.
                        if(candidates.Count == 0 && requestedCategory != null){
                            candidates = fortunePool.FindAll(f => f.Category.Equals(requestedCategory,StringComparison.OrdinalIgnoreCase));
                        }
                        //if it's still empty (or if no category is specified), select randomly from the entire pool
                        if(candidates.Count == 0){
                            candidates = fortunePool;
                        }
                        //pick a fortune
                        Fortune picked =candidates[rnd.Next(candidates.Count)];

                        //emoji
                        string emoji = picked.Category.ToLower() switch
                        {
                            "motivational" => "💪",
                            "success"      => "🏆",
                            "social"       => "🤝",
                            "wise"         => "🦉",
                            "future"       => "🔮",
                            _              => "🌟" 
                        };
                        //we send three lines of data to the client: Rarity/Category, Fortune Text, and Lucky Numbers
                        writer.WriteLine($"{emoji}[{picked.Rarity.ToUpper()}] Category:{picked.Category}");
                        writer.WriteLine($"Fortune: {picked.Text}");
                        string luckyNums = $"{rnd.Next(1,56)},{rnd.Next(1, 56)}, {rnd.Next(1, 56)}";
                        writer.WriteLine($"Lucky Numbers: {luckyNums}");

                        Console.WriteLine($"[LOG] {selectedRarity} fortune sent to client. Category: {picked.Category}");
                    }
                    
                
                    else if(request.ToUpper()== "UPLOAD"){
                        Console.WriteLine("[UPLOAD] File Transfer is Starting....");
                        //read the file size(4 byte)
                        byte[] sizeBuffer = new byte [4];
                        stream.Read(sizeBuffer, 0, 4);
                        int fileSize = BitConverter.ToInt32(sizeBuffer, 0);
                        Console.WriteLine($"[UPLOAD] Expected file size: {fileSize} byte");
                        
                        //read the file content byte by byte
                        byte[] fileData = new byte[fileSize];
                        int totalRead = 0;
                        while (totalRead < fileSize){
                            int read = stream.Read(fileData, totalRead, fileSize - totalRead);
                            if (read == 0) break;
                            totalRead += read;
                        }
                        string newContent = System.Text.Encoding.UTF8.GetString(fileData);
                        string[] newLines = newContent.Split(new[]{"\r\n","\r","\n"}, StringSplitOptions.RemoveEmptyEntries);
                        //lock, prevents race conditions when multiple clients
                        //attempt to upload files simultaneously.
                        lock(fortunePool) {
                            foreach (var line in newLines){
                                var p = line.Split('|');
                                if (p.Length == 3)
                                    fortunePool.Add(new Fortune { Category = p[0].Trim(), Rarity = p[1].Trim(), Text = p[2].Trim() }); // Text eklendi
                                else
                                    fortunePool.Add(new Fortune { Category ="General", Rarity ="Common", Text = line.Trim() });
                            }
                        }
                        writer.WriteLine($"SUCCESS: {newLines.Length} new fortunes added!");
                    }
                    else if (upperRequest =="EXIT") break;
                }    
            }
                        
            catch(IOException){// triggered,if a client suddenly shut down the program 
            Console.WriteLine("⚠️A client forced the connection to close. ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client logic error: " + ex.Message);
            }
        // using blocks will close the stream automatically
            finally{
                lock(clientMap){clientMap.Remove(myId);} //exit from the list when connection lost
                client.Close();
                Console.WriteLine("A client has disconnected.");
            }
        }
    }
    static void BroadcastDailyFortune(){

        using (UdpClient udpServer = new UdpClient())
        {
            // decides where the data goes
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("239.0.0.1"), 5001); //we can think 5001 as a door number which the client listens
            
            string dailyFortune = "📢 FORTUNE OF THE DAY!!!!:" + fortunePool[rnd.Next(fortunePool.Count)].Text;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(dailyFortune);

            udpServer.Send(data, data.Length, remoteEP); //for real time updates, we use udp
            Console.WriteLine("Daily fortune broadcasted via UDP.");
        }
    }
}//end of  class
