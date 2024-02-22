using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

namespace WindowsGSM.Plugins
{
    public class MOE : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.MOE", // WindowsGSM.XXXX
            author = "Sarpendon, NeXuS mod",
            description = "WindowsGSM plugin for supporting Myth of Empires Dedicated Server",
            version = "1.9a mod",
            url = "https://github.com/JTNeXuS2/MOE.cs", // Github repository link (Best practice)
            color = "#8802db" // Color Hex
        };

        // - Standard Constructor and properties
        public MOE(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "1794810"; // Game server appId, Myth of Empires is 1794810


        // - Game server Fixed variables
        public override string StartPath => @"MOE\Binaries\Win64\MOEServer.exe"; // Game server start path
        public string FullName = "MOE Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 10; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        // - Game server default values
        public string Port = "9900"; // Default port
        public string QueryPort = "9901"; // Default query port
        public string Defaultmap = "LargeTerrain_Central_Main"; // Used for Server ID
        public string ShutDownServicePort;
        public string Maxplayers = "100"; // Default maxplayers
        public string Additional => GetAdditional();
        public string FileAdditional => ReadServerParamConfig();

        private string GetAdditional()
        {
            string shutDownServicePort = (int.Parse(_serverData.ServerQueryPort) + 1).ToString();
            return $"-DBLogEnable=1 -log log=123456789.log -LOCALLOGTIMES -PrivateServer -NotCheckServerSteamAuth -EnableVACBan=0 -MultiHome={_serverData.ServerIP} -bStartShutDownServiceInPrivateServer=true -ShutDownServiceIP=\"0.0.0.0\"  -ShutDownServicePort=\"{shutDownServicePort}\" -ShutDownServiceKey=\"123456789\" -pakdir=*..\\WindowsPrivateServer\\MOE\\123456789\\Mods* -Description=\"discord.gg/qYmBmDR\" -GameServerPVPType=1 -NoticeSelfEnable=true -NoticeSelfEnterServer=\"наш дискорд discord.gg/qYmBmDR\" -MapDifficultyRate=1 -ForceSteamNet=1 -ServerId=100 -ClusterId=1 -ServerAdminAccounts=\"76561198277462764;76561198838209834\" -NoticeAllEnable=true -disable_qim -SaveGameIntervalMinute=10 -config=ServerParamConfig_123456789.ini";
        }

        private string ReadServerParamConfig()
        {
            string serverParamConfigPath = ServerPath.GetServersServerFiles(_serverData.ServerID, "ServerParamConfig_123456789.ini");
            StringBuilder fileContent = new StringBuilder();
            if (File.Exists(serverParamConfigPath))
            {
                using (StreamReader reader = new StreamReader(serverParamConfigPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();  // Удаляем лишние пробелы в начале и конце строки
                        if (!string.IsNullOrEmpty(line) && line != "[BaseServerConfig]")
                        {
                            fileContent.Append($" -{line}"); // Добавляем " -" перед каждой строкой, кроме "[BaseServerConfig]" и пустых строк
                        }
                    }
                }
            }
            return fileContent.ToString();
        }


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
             
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {

            //Get WAN IP from net
            //string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            //var externalIp = IPAddress.Parse(externalIpString);

            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Prepare start parameter
            var param = new StringBuilder();
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : $"{_serverData.ServerMap} -game -server -DataLocalFile");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerIP) ? string.Empty : $" -OutAddress={_serverData.ServerIP}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -port={_serverData.ServerPort}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -QueryPort={_serverData.ServerQueryPort}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $" -name=\"{_serverData.ServerName}\"");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $" -SessionName=\"{_serverData.ServerName}\"");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" -MaxPlayers={_serverData.ServerMaxPlayer}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerGSLT) ? string.Empty : $" -PrivateServerPassword={_serverData.ServerGSLT}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}");
            param.Append(FileAdditional); // Добавляем считанные из файла параметры запуска

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
                    //Arguments = param,
					Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

		// - Stop server function
     public async Task Stop(Process p)
		{
			await Task.Run(() =>
			{
				Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
				Functions.ServerConsole.SendWaitToMainWindow("SaveWorld"); // Execute SaveGame command
				System.Threading.Thread.Sleep(5000); // Wait for 10 seconds (in milliseconds)
				Functions.ServerConsole.SendWaitToMainWindow("^c"); // Send Ctrl+C command
				p.WaitForExit(5000);
			});
		}


        // - Update server function
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
