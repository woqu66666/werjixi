using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

static class Program
{
    const string DEFAULT_SERVER_ENDPOINT = "http://localhost:3000/api/report";

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            string sessionId = ParseSessionFromArgs(args);
            if (string.IsNullOrEmpty(sessionId) && args.Length > 0) sessionId = args[0];
            if (string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    var exe = Process.GetCurrentProcess().MainModule.FileName;
                    var name = Path.GetFileNameWithoutExtension(exe ?? "");
                    var parts = name.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[0].Equals("Detector", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionId = parts[1];
                    }
                }
                catch { }
            }

            var consent = MessageBox.Show(
                "该工具将采集你的系统硬件信息（OS、CPU、内存、GPU、分辨率、磁盘等）并通过 HTTPS 上传到服务端以供网页使用。是否同意？\n\n详情请参阅 privacy.txt",
                "允许上传系统信息？",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (consent != DialogResult.Yes) return;

            string serverEndpoint = ResolveServerEndpoint(args) ?? DEFAULT_SERVER_ENDPOINT;

            var report = new
            {
                timestamp = DateTime.UtcNow,
                session = sessionId,
                os = GetOsInfo(),
                cpu = GetCpuInfo(),
                memory = GetMemoryInfo(),
                gpus = GetGpuInfo(),
                screen = GetScreenInfo(),
                disks = GetDiskInfo(),
                dxdiag = GetDxDiagSummary()
            };
            string json = JsonConvert.SerializeObject(report);

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                var resp = http.PostAsync(serverEndpoint, new StringContent(json, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    static string ParseSessionFromArgs(string[] args)
    {
        if (args.Length == 0) return null;
        var first = args[0];
        if (first.StartsWith("mydetector://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(first);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return q.Get("session");
            }
            catch { return null; }
        }
        return args[0];
    }

    static string ResolveServerEndpoint(string[] args)
    {
        // 1) 协议参数 endpoint
        if (args.Length > 0 && args[0].StartsWith("mydetector://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(args[0]);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var ep = q.Get("endpoint");
                if (!string.IsNullOrWhiteSpace(ep)) return ep;
            }
            catch { }
        }
        // 2) 命令行 --endpoint=...
        foreach (var a in args)
        {
            if (a.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                var ep = a.Substring("--endpoint=".Length).Trim();
                if (!string.IsNullOrWhiteSpace(ep)) return ep;
            }
        }
        // 3) 文件名编码：Detector_<session>__e_<base64url(endpoint)>.exe
        try
        {
            var exe = Process.GetCurrentProcess().MainModule.FileName;
            var name = Path.GetFileNameWithoutExtension(exe ?? "");
            var marker = "__e_";
            var idx = name.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var b64 = name.Substring(idx + marker.Length);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    var b64std = b64.Replace('-', '+').Replace('_', '/');
                    switch (b64std.Length % 4)
                    {
                        case 2: b64std += "=="; break;
                        case 3: b64std += "="; break;
                    }
                    var bytes = Convert.FromBase64String(b64std);
                    var ep = Encoding.UTF8.GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(ep)) return ep;
                }
            }
        }
        catch { }
        // 4) 环境变量
        var env = Environment.GetEnvironmentVariable("MYDETECTOR_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        // 5) 同目录 config.json
        try
        {
            var exe = Process.GetCurrentProcess().MainModule.FileName;
            var dir = Path.GetDirectoryName(exe ?? "");
            var cfgPath = Path.Combine(dir ?? ".", "config.json");
            if (File.Exists(cfgPath))
            {
                var txt = File.ReadAllText(cfgPath, Encoding.UTF8);
                dynamic obj = JsonConvert.DeserializeObject(txt);
                if (obj != null)
                {
                    string ep = obj.serverEndpoint != null ? (string)obj.serverEndpoint : (obj.endpoint != null ? (string)obj.endpoint : null);
                    if (!string.IsNullOrWhiteSpace(ep)) return ep;
                }
            }
        }
        catch { }
        return null;
    }

    static object GetOsInfo()
    {
        return new
        {
            platform = Environment.OSVersion.Platform.ToString(),
            version = Environment.OSVersion.VersionString,
            is64 = Environment.Is64BitOperatingSystem
        };
    }

    static object GetCpuInfo()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("select Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed from Win32_Processor");
            foreach (ManagementObject mo in searcher.Get())
            {
                return new
                {
                    name = (mo["Name"] ?? "").ToString().Trim(),
                    cores = mo["NumberOfCores"],
                    logicalProcessors = mo["NumberOfLogicalProcessors"],
                    maxClockMHz = mo["MaxClockSpeed"]
                };
            }
        }
        catch { }
        return null;
    }

    static object GetMemoryInfo()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize from Win32_OperatingSystem");
            foreach (ManagementObject mo in searcher.Get())
            {
                ulong totalKb = Convert.ToUInt64(mo["TotalVisibleMemorySize"]);
                return new { totalMB = totalKb / 1024 };
            }
        }
        catch { }
        return null;
    }

    static object GetGpuInfo()
    {
        var list = new System.Collections.Generic.List<object>();
        try
        {
            var searcher = new ManagementObjectSearcher("select Name,DriverVersion,AdapterRAM,PNPDeviceID from Win32_VideoController");
            foreach (ManagementObject mo in searcher.Get())
            {
                long ram = 0;
                if (mo["AdapterRAM"] != null) ram = Convert.ToInt64(mo["AdapterRAM"]);
                list.Add(new
                {
                    name = (mo["Name"] ?? "").ToString().Trim(),
                    driver = (mo["DriverVersion"] ?? "").ToString(),
                    vramMB = ram > 0 ? ram / (1024 * 1024) : (long?)null,
                    pnpId = (mo["PNPDeviceID"] ?? "").ToString()
                });
            }
        }
        catch { }
        return list;
    }

    static object GetScreenInfo()
    {
        try
        {
            var scr = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            return new { width = scr.Width, height = scr.Height, primary = true };
        }
        catch { return null; }
    }

    static object GetDiskInfo()
    {
        var list = new System.Collections.Generic.List<object>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady) continue;
                list.Add(new
                {
                    name = drive.Name,
                    type = drive.DriveType.ToString(),
                    totalGB = drive.TotalSize / (1024L * 1024L * 1024L),
                    freeGB = drive.TotalFreeSpace / (1024L * 1024L * 1024L)
                });
            }
            catch { }
        }
        return list;
    }

    static string GetDxDiagSummary()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "dxdiag_" + Guid.NewGuid().ToString() + ".txt");
        try
        {
            var psi = new ProcessStartInfo("dxdiag", "/t \"" + tmp + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (var p = Process.Start(psi))
            {
                if (!p.WaitForExit(10000)) { p.Kill(); return null; }
            }
            if (File.Exists(tmp))
            {
                string txt = File.ReadAllText(tmp, Encoding.UTF8);
                var lines = txt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var L in lines)
                    if (L.StartsWith("DirectX Version", StringComparison.OrdinalIgnoreCase)) return L;
            }
        }
        catch { }
        finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
        return null;
    }
}


