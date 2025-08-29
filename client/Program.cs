using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using System.Threading.Tasks;

class Program
{
    // 默认上报地址（用于兜底）
    const string DEFAULT_SERVER_ENDPOINT = "http://localhost:3000/api/report";

    [STAThread]
    static async Task Main(string[] args)
    {
        try
        {
            string sessionId = ParseSessionFromArgs(args);
            string serverEndpoint = ResolveServerEndpoint(args) ?? DEFAULT_SERVER_ENDPOINT;
            if (string.IsNullOrEmpty(sessionId))
            {
                // 也允许用户直接从命令行传 session id
                if (args.Length > 0) sessionId = args[0];
                // 从自身文件名推断：Detector_<session>.exe
                if (string.IsNullOrEmpty(sessionId))
                {
                    try
                    {
                        var exe = Process.GetCurrentProcess().MainModule?.FileName;
                        var name = Path.GetFileNameWithoutExtension(exe ?? "");
                        // 允许 Detector-xxx 或 Detector_xxx
                        var parts = name.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0].Equals("Detector", StringComparison.OrdinalIgnoreCase))
                        {
                            sessionId = parts[1];
                        }
                    }
                    catch { }
                }
            }

            // 控制台模式：直接采集并上报（无需交互）

            Console.WriteLine("采集系统信息…");
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

            string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = false });

            Console.WriteLine("上报到： " + serverEndpoint);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await http.PostAsync(serverEndpoint, content);
            Console.WriteLine("Server response: " + resp.StatusCode);

            // 如果 sessionId 被带入，尝试在页面主动返回时会读取到服务端保存的结果
        }
        catch (Exception ex)
        {
            Console.WriteLine("异常: " + ex);
        }
    }

    static string ParseSessionFromArgs(string[] args)
    {
        if (args.Length == 0) return null;
        // 当通过 mydetector://... 协议唤起时，通常 args[0] = "mydetector://start?session=xxxx"
        var first = args[0];
        if (first.StartsWith("mydetector://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(first);
                var q = QueryHelpers.ParseQuery(uri.Query);
                return q.TryGetValue("session", out var vals) ? vals.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
        // 支持直接传 session id
        return args[0];
    }

    static string ResolveServerEndpoint(string[] args)
    {
        // 1) 协议参数中带 endpoint
        if (args.Length > 0 && args[0].StartsWith("mydetector://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(args[0]);
                var q = QueryHelpers.ParseQuery(uri.Query);
                if (q.TryGetValue("endpoint", out var vals))
                {
                    var ep = vals.ToString();
                    if (!string.IsNullOrWhiteSpace(ep)) return ep;
                }
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
        // 3) 环境变量
        var env = Environment.GetEnvironmentVariable("MYDETECTOR_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        // 4) 文件名编码：Detector_<session>__e_<base64url(endpoint)>.exe
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            var name = Path.GetFileNameWithoutExtension(exe ?? "");
            // 解析 __e_<b64url>
            var marker = "__e_";
            var idx = name.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var b64 = name.Substring(idx + marker.Length);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    // base64url -> base64
                    var base64 = b64.Replace('-', '+').Replace('_', '/');
                    switch (base64.Length % 4)
                    {
                        case 2: base64 += "=="; break;
                        case 3: base64 += "="; break;
                    }
                    var bytes = Convert.FromBase64String(base64);
                    var ep = Encoding.UTF8.GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(ep)) return ep;
                }
            }
        }
        catch { }
        // 5) 同目录 config.json { "serverEndpoint": "https://..." }
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            var dir = Path.GetDirectoryName(exe ?? "");
            var cfgPath = Path.Combine(dir ?? ".", "config.json");
            if (File.Exists(cfgPath))
            {
                var txt = File.ReadAllText(cfgPath, Encoding.UTF8);
                using var doc = JsonDocument.Parse(txt);
                if (doc.RootElement.TryGetProperty("serverEndpoint", out var val) && val.ValueKind == JsonValueKind.String)
                {
                    var ep = val.GetString();
                    if (!string.IsNullOrWhiteSpace(ep)) return ep;
                }
                if (doc.RootElement.TryGetProperty("endpoint", out var val2) && val2.ValueKind == JsonValueKind.String)
                {
                    var ep = val2.GetString();
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
                return new
                {
                    totalMB = totalKb / 1024
                };
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
            // 控制台版本为减少体积，不依赖 Windows.Forms，屏幕信息返回 null
            return null;
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
        // 调用 dxdiag /t 输出到临时文件并解析 DirectX 版本与显卡部分信息（简要）
        string tmp = Path.Combine(Path.GetTempPath(), "dxdiag_" + Guid.NewGuid().ToString() + ".txt");
        try
        {
            var psi = new ProcessStartInfo("dxdiag", $"/t \"{tmp}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (var p = Process.Start(psi))
            {
                if (!p.WaitForExit(10000)) // 最多等待 10s
                {
                    p.Kill();
                    return null;
                }
            }
            if (File.Exists(tmp))
            {
                string txt = File.ReadAllText(tmp, Encoding.UTF8);
                // 尝试找 DirectX Version
                var lines = txt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string dx = null;
                foreach (var L in lines)
                {
                    if (L.StartsWith("DirectX Version", StringComparison.OrdinalIgnoreCase))
                    {
                        dx = L;
                        break;
                    }
                }
                // 返回整段（小尾巴）
                return dx ?? null;
            }
        }
        catch { }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
        return null;
    }
}



