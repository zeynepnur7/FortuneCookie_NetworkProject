# 🥠 Fortune Cookie Network 
A multi-threaded, real-time client-server application developed in **C#** that combines the fun of fortune-telling with instant messaging and file sharing. This project demonstrates advanced network programming concepts including **TCP/IP** communication, **UDP Multicast**, and asynchronous task handling.

---
## 🌟Core Features

This project is built upon four core commands and two distinct network protocols:
1. Smart Fortune System (`GET_FORTUNE`)
    - Probability-Based Selection: Fortunes are not chosen purely at random; they are selected based on specific probability calculations:
        - Common ($P = 70\%$): Standard fortunes.
        - Rare ($P = 25\%$): Less frequent, Cyan-colored fortunes.
        - Legendary ($P = 5\%$): Rare, Golden-yellow colored fortunes with special effects.
    - Category Filtering: Users can request fortunes from specific categories by using commands such as GET_FORTUNE wise.
2. ID-Based Instant Messaging (`SEND`)
    - Routing: The server assigns a unique ID to each client and routes text messages to the target recipient via the SEND <ID> <Message> command.
    - Asynchronous Notification: Thanks to the background listener, messages appear instantly on the recipient's screen without requiring any action from the user at that         moment.
3. Dynamic Fortune Upload (`UPLOAD`)
    - TCP File Transfer: A local .txt file is packaged and sent to the server, starting with a 4-byte header for the file size followed by the actual content.
    - Live Update: Uploaded fortunes are added to the fortune pool immediately, without the need to restart the server.
4. Personal Fortune History (`MY_FORTUNE_HISTORY`)
    - Session Recording: The client stores all received fortunes, including timestamps, in the my_fortune_history list as long as the application remains open.
5. Daily Fortune Broadcast (`UDP Multicast`)
    - Global Broadcast: Every **60 seconds**, the server sends a shared "Fortune of the Day" message to all connected clients simultaneously via UDP Multicast.

## 💻 Technical Requirements & Dependencies
To build and run the Fortune Cookie Network, ensure your environment meets the following specifications:
1. Framework & Runtime
-Target Framework: .NET 8.0 SDK or later.
-Language: C# 12.0+.
-Platform:Support for Windows.
2. Core Dependencies (Namespaces)
The project utilizes several built-in .NET libraries to handle core functionalities:
- `System.Net.Sockets`: Used for managing TCP connections for commands and UDP Multicast for broadcasts.
- `System.Threading.Tasks`: Essential for handling asynchronous operations, such as background message listening.
- `System.IO`: Required for reading/writing local fortune files and processing network streams via StreamReader and StreamWriter.
- `System.Text`: Used for UTF-8 encoding to ensure emojis and special characters display correctly in the terminal.
- `FortuneCookie.Common`: A project-specific shared library containing the Fortune model.
3. Network Configuration
-TCP Port 5000: Used for the primary command-and-control connection between the client and server.
-UDP Port 5001: Reserved for receiving the asynchronous "Fortune of the Day" broadcasts.
-Multicast IP (239.0.0.1): The specific group address used for UDP Multicast broadcasting.

## 🚀 How to Run
Follow these steps to initialize the network and connect multiple clients.
1. Setup
-Before running the applications, clone or copy the project into a local folder.
-Ensure **.NET SDK 8.0** is installed.
2. Start the Fortune Server
The server must be active before any clients can connect.
In the Terminal:
```bash
cd server
dotnet run
```
The server will load fortunes from fortunes.txt and begin listening for TCP connections on port 5000 and broadcasting UDP messages on port 5001.

3. Start the First Client
-Open a new terminal window.
-Navigate to the Client directory:
In the Terminal:
```bash
cd client
dotnet run
```

Upon success, you will see your unique Client ID assigned by the server.

4. Start Additional Clients (Important!)
To run a second or third client;
-Ensure the server logs "A client has connected" for every new window.
-Open another terminal window and follow the same steps as the first client.

## 🛠️ How to Test Core Features?
Once your terminals are set up, verify the following:
-Multicast: Wait 60 seconds to see if the [BROADCAST] message appears in all client terminals.
-Messaging: Try SEND <ID> <msg> from one client to another to test the Magenta-colored notification system.
-Uploading: Write new fortunes in the new_fortunes.txt file.Then type 'UPLOAD' on the Terminal. When you upload new fortunes to the fortune pool,you will get a confirmation message.
-Get History: When you get fortunes with the GET_FORTUNE command, your fortunes will be appended to a list. Type MY_FORTUNE_HISTORY,you will see your past fortunes as a list with timestamps.

## 📂Project Structure
```text
fortuneCookie/
├── README.md         
├── client/                         
│   ├── FortuneClient.cs            
│   ├── Client.csproj               
│   └── new_fortunes.txt            
├── server/                         
│   ├── Server.cs                   
│   ├── Server.csproj              
│   └── fortunes.txt               
└── common/           
    ├── Fortune.cs                  
    └── Common.csproj
```

## ⚠️ Known Limitations & Issues
While the Fortune Cookie Network is fully functional for local testing, there are specific technical constraints and known behaviors to keep in mind:
1. Windows Binary Locking (MSB3021)
-Description: When running multiple client instances from the same directory using dotnet run, the .NET SDK attempts to rebuild the project and overwrite the Client.exe.
-Impact: Since the first client process is already using the .exe, Windows locks the file, causing a build failure for subsequent instances.
-Workaround: Users must launch clients one by one, or use the `dotnet run --no-build` command for additional clients to bypass the compilation phase.
2. Hardcoded Absolute File Paths
-Description: The file paths for fortunes.txt (Server) and new_fortunes.txt (Client) are currently hardcoded as absolute path
-Impact: The application may fail to load or upload files if run on a different machine or user profile without manual path adjustment in the source code.
-Future Fix: These should be transitioned to relative paths using AppDomain.CurrentDomain.BaseDirectory.
3. UDP Multicast Network Restrictions
-Description: The "Fortune of the Day" broadcast relies on UDP Multicast at address 239.0.0.1.
-Impact: Some network environments, such as public Wi-Fi, corporate firewalls, or active VPNs, may block multicast traffic, preventing the broadcast from appearing in the client terminal.
4. In-Memory Data Volatility
-Description: Both the server-side clientMap and the client-side my_fortune_history are stored as in-memory collections (Dictionary and List).
-Impact: All data, including client IDs and received fortune history, is lost once the respective applications are terminated. No persistent database (e.g., SQL or JSON) is currently implemented.
5. Lack of Encryption
-Description: All communication between the client and server is transmitted as plain text over NetworkStream.
-Impact: The system is intended for local network testing only. It is vulnerable to packet sniffing if deployed over a public or unsecured network.

## 🎖️ Credits
- Implementation:This project was developed as part of the Network Programming course curriculum by Zeynep Nur Erten.
- Patterns: The network architecture and patterns implemented in this project were inspired by and modeled after the principles found in: `Blum, R. (2003). C# Network Programming. Sybex`.
- AI Assistance: 
    - Gemini AI: Built with help from Gemini Pro.
