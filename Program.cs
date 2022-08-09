using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ConsoleApp2
{
    public class ServerInfo
    {
        public HardDriver[] hardInfo { get; set; }
        public string TotalMemory { get; set; }

        public CPUInfo[] cpuInfo { get; set; }
        public string[] installSoftware { get; set; }

        public SiteInfo[] siteInfos { get; set; } 
    }
    
    public class CPUInfo
    {
        public string CoreID { get; set; }
        public string ClockSpeed { get; set; }
        public string Usage { get; set; }
    }

    public class HardDriver
    {
        public string DriverName { get; set; }
        
        public string TotalHardDisk { get; set; }

        public string AvailableDisk { get; set; }

        public string UsedDisk { get; set; }

    }

    public class SiteInfo
    {
        public string siteName { get; set; }

        public string rootPath { get; set; }

        public string resultShow { get; set; } 
    }

    public class Class1
    {
        static HardDriver[] hardStr;
        static string ramStr;
        static CPUInfo[] cpuStr;
        static List<string> software;
        static SiteInfo[] siteStr;

        static void Main(string[] args)
        {

            // Get HardDisk Data                
            GetUsedDisk();
            // Get RAM Data
            ramStr = FormatSize(GetTotalPhys());
//          Console.WriteLine("\nTotal memory:" + FormatSize(GetTotalPhys()) + "\n");
            //Console.WriteLine("It can be used:" + FormatSize(GetAvailPhys()));
            //Console.WriteLine("Available use:" + FormatSize(GetUsedPhys()));
            
            GetUsedCPU();

            //Console.WriteLine("\nInstalled Softwares:");
            GetProgramList();

            GetIISSiteList();

            var serverInfoData = new ServerInfo
            {
                hardInfo = hardStr,
                TotalMemory = ramStr,
                cpuInfo = cpuStr,
                installSoftware = software.ToArray(),
                siteInfos = siteStr,
            };

            JsonSerializerOptions jso = new JsonSerializerOptions { WriteIndented = true };
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            string jsonString = System.Text.Json.JsonSerializer.Serialize(serverInfoData, jso);

            string path = Environment.CurrentDirectory + "test.json";

            try
            {
                // Create the file, or overwrite if the file exists.
                using (FileStream fs = File.Create(path))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }

                Console.WriteLine(jsonString);
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.ReadLine();
        }

        public static void GetIISSiteList()
        {
            var iisManager = new ServerManager();
            SiteCollection sites = iisManager.Sites;

            Console.WriteLine("\n");

            siteStr = new SiteInfo[sites.Count()];

            int count = 0;
            foreach (Site site in sites)
            {
//              Console.WriteLine("Site Name:" + site.Name);
                var applicationRoot = site.Applications.Where(a => a.Path == "/").Single();
                var virtualRoot = applicationRoot.VirtualDirectories.Where(v => v.Path == "/").Single();

                string path = virtualRoot.PhysicalPath;
                path = path.Replace("%SystemDrive%", "C:");
//                Console.WriteLine("Physical Path: " + path);
//                Console.WriteLine("Result: " + GetSiteInfo(path));

                SiteInfo temp = new SiteInfo();
                temp.siteName = site.Name;
                temp.rootPath = path;
                temp.resultShow = GetSiteInfo(path);
                siteStr[count++] = temp;       
            }
        }

        public static string GetSiteInfo(string path)
        {
            string result = "";
            // get the list of files in the root directory and all its subdirectories
            string parentPath = System.IO.Directory.GetParent(path).FullName;

            string[] filesA = Directory.GetFiles(parentPath, "*.*", SearchOption.AllDirectories);

            string[] filesB = Directory.GetFiles(parentPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".mp3") || s.EndsWith(".jpg") || s.EndsWith(".svg") || s.EndsWith(".ico") || s.EndsWith(".html") || s.EndsWith(".js") || s.EndsWith(".css") || s.EndsWith(".avi") || s.EndsWith(".mp4") || s.EndsWith(".bmp") || s.EndsWith(".png")).ToArray();

            string[] filesC = Directory.GetFiles(parentPath, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".exe") || s.EndsWith(".dll") || s.EndsWith("asp.net")).ToArray();

            string[] filesDll = Directory.GetFiles(parentPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".dll")).ToArray();
            
            if (filesA.Length == 0)
            {
                result ="orphan server";
            }
            else if(filesA.Length == filesB.Length)
            {
                result = "static content";
            }
            else if(filesDll.Length > 1)
            {
                bool status = false;
                string[] filesConfig = Directory.GetFiles(parentPath, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".json") || s.Equals(".config")).ToArray();
                for(int i = 0; i < filesConfig.Length; i++)
                {
                    string text = System.IO.File.ReadAllText(filesConfig[i]);
                    status = text.Contains("onnection");
                    if(status)
                    {
                        result = "dynamic and Very complex application";
                        return result;
                    }
                }
                result = "dynamic and complex application";
            }
            return result;
        }
        public static void GetUsedDisk() // This one working fine
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            hardStr = new HardDriver[drives.Count()];

            int cnt = 0;
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    HardDriver temp = new HardDriver();
                    temp.DriverName = drive.Name[0].ToString();
                    temp.TotalHardDisk = (drive.TotalSize / 1024 / 1024 / 1024) + "GB";
                    temp.AvailableDisk = (drive.AvailableFreeSpace / 1024 / 1024 / 1024) + "GB";
                    temp.UsedDisk = ((drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024) + "GB";
                    hardStr[cnt] = temp;
                    cnt++;
//                  Console.WriteLine("Driver Name : " + drive.Name + "\nTotal HardDisk:" + (drive.TotalSize / 1024 / 1024 / 1024) + " GB\n" + "Available HardDisk:" + (drive.AvailableFreeSpace / 1024 / 1024 / 1024) + " GB\n" + "Used HardDisk:" + ((drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024) + " GB");
                }
            }
        }

        public static void GetProgramList()
        {
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                software = new List<string>();
                foreach (string skName in rk.GetSubKeyNames())
                {

                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        try
                        {

                            var displayName = sk.GetValue("DisplayName");
                            var size = sk.GetValue("EstimatedSize");

                            if (displayName != null)
                            {
                                software.Add(displayName.ToString());
                                //Console.WriteLine(displayName);
                            }
                        }
                        catch (Exception ex)
                        { }
                    }
                }
            }
        }
        // This one not working It always gives 0% Please Help 
        public static void GetUsedCPU()
        {
            PerformanceCounter[] cpuCounter = new PerformanceCounter[Environment.ProcessorCount];

            for (int i = 0; i < cpuCounter.Length; i++)
            {
                PerformanceCounter temp = new PerformanceCounter();
                temp.CategoryName = "Processor";
                temp.CounterName = "% Processor Time";
                temp.InstanceName = i.ToString();

                cpuCounter[i] = temp;
            }

            for (int i = 0; i < cpuCounter.Length;i++)
            {
                int temp = (int)cpuCounter[i].NextValue();
            }

            System.Threading.Thread.Sleep(1000);

            var searcher = new ManagementObjectSearcher("select MaxClockSpeed from Win32_Processor");

            cpuStr = new CPUInfo[cpuCounter.Length];

            foreach (var item in searcher.Get())
            {
                for (int i = 0; i < cpuCounter.Length; i++)
                {
                    CPUInfo temp = new CPUInfo();

                    var speed = Math.Round(0.001f * (uint)item["MaxClockSpeed"], 1);
                    temp.CoreID = "Core " + i.ToString();
                    temp.ClockSpeed = speed.ToString() + "GHz";
                    temp.Usage = cpuCounter[i].NextValue().ToString() + "%";
                    cpuStr[i] = temp;

//                    Console.WriteLine("Computer CPU Core " + i + " Speed:" + speed + "GHz " + cpuCounter[i].NextValue() + "%");
                }
            }
        }
        #region Obtain memory information API
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        //Define the information structure of memory
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_INFO
        {
            public uint dwLength; //Current structure size
            public uint dwMemoryLoad; //Current memory utilization
            public ulong ullTotalPhys; //Total physical memory size
            public ulong ullAvailPhys; //Available physical memory size
            public ulong ullTotalPageFile; //Total Exchange File Size
            public ulong ullAvailPageFile; //Total Exchange File Size
            public ulong ullTotalVirtual; //Total virtual memory size
            public ulong ullAvailVirtual; //Available virtual memory size
            public ulong ullAvailExtendedVirtual; //Keep this value always zero
        }
        #endregion

        #region Formatting capacity size
        /// <summary>
        /// Formatting capacity size
        /// </summary>
        /// <param name="size">Capacity ( B)</param>
        /// <returns>Formatted capacity</returns>
        private static string FormatSize(double size)
        {
            double d = (double)size;
            int i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }
            string[] unit = { "B", "KB", "MB", "GB", "TB" };
            return (string.Format("{0} {1}", Math.Round(d, 2), unit[i]));
        }
        #endregion

        #region Get the current memory usage
        /// <summary>
        /// Get the current memory usage
        /// </summary>
        /// <returns></returns>
        public static MEMORY_INFO GetMemoryStatus()
        {
            MEMORY_INFO mi = new MEMORY_INFO();
            mi.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(mi);
            GlobalMemoryStatusEx(ref mi);
            return mi;
        }
        #endregion

        #region Get the current available physical memory size
        /// <summary>
        /// Get the current available physical memory size
        /// </summary>
        /// <returns>Current available physical memory( B)</returns>
        public static ulong GetAvailPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }
        #endregion

        #region Get the current memory size used
        /// <summary>
        /// Get the current memory size used
        /// </summary>
        /// <returns>Memory size used( B)</returns>
        public static ulong GetUsedPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }
        #endregion

        #region Get the current total physical memory size
        /// <summary>
        /// Get the current total physical memory size
        /// </summary>
        /// <returns&amp;gt;Total physical memory size( B)&amp;lt;/returns&amp;gt;
        public static ulong GetTotalPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullTotalPhys;
        }
        #endregion
    }
}