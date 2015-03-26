using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace SG_1_Encoder
{
    class Program
    {
        static Process hbproc;
        static int scanProcessID;
        static DVD thisDVD;

        static Regex sg1 = new Regex("(?:STARGATE_)?SG1_V([1-9])_R1_YR([0-9]{1,2})");

        static void Main(string[] args)
        {

            Dictionary<string, string> seriesFolders = new Dictionary<string, string>();
            seriesFolders.Add("(?:STARGATE_)?SG1_V(?<minor>[1-9])_R1_YR(?<major>[0-9]{1,2})", "Stargate SG-1");
            //seriesFolders.Add("STARGATE_ATLANTIS_S(?<major>[1-5])_D(?<minor>[1-9])", "Stargate Atlantis");

            foreach (string key in seriesFolders.Keys)
            {

                Regex dvdNameExpression = new Regex(key);

                DirectoryInfo dvdsDirectory = new DirectoryInfo(@"D:\Users\Jonathan\Documents\DVDFab\FullDisc");
                List<DirectoryInfo> dvdDirectories = new List<DirectoryInfo>(dvdsDirectory.GetDirectories());



                for (int i = dvdDirectories.Count - 1; i >= 0; i--)
                    if (!dvdNameExpression.IsMatch(dvdDirectories[i].Name))
                        dvdDirectories.Remove(dvdDirectories[i]);


                Sorter s = new Sorter() { R = dvdNameExpression };
                dvdDirectories.Sort(s);


                int episode = 1;
                int? previousSeason = 8;

                DirectoryInfo seriesDirectory = new DirectoryInfo(Path.Combine(@"D:\Users\Jonathan\Videos\TV Shows", seriesFolders[key]));
                if (!seriesDirectory.Exists)
                    seriesDirectory.Create();

                foreach (DirectoryInfo d in dvdDirectories)
                {
                    Match m = dvdNameExpression.Match(d.Name);

                    int season = int.Parse(m.Groups["major"].Value);
                    int volume = int.Parse(m.Groups["minor"].Value);

                    if (season >= previousSeason)
                    {
                        if (season != previousSeason)
                            episode = 1;

                        DirectoryInfo seasonDirectory = new DirectoryInfo(seriesDirectory.FullName + @"\Season " + season.ToString("00"));
                        if (!seasonDirectory.Exists)
                            seasonDirectory.Create();

                        scanProcess(null, d.FullName);
                        foreach (Title t in thisDVD.Titles)
                            if (t.Duration > new TimeSpan(0, 40, 0))
                            {
                                string fileName =  seriesFolders[key] + " S" + season.ToString("00") + "E" + episode.ToString("00") + ".m4v";
                                fileName = seasonDirectory.FullName + "\\" + fileName;

                                FileInfo[] h = seasonDirectory.GetFiles(episode.ToString("00") + "*.m4v");
                                if (!File.Exists(fileName) && seasonDirectory.GetFiles(episode.ToString("00") + "*.m4v").Length == 0)
                                {
                                    Process handBrake = new Process();
                                    handBrake.StartInfo.FileName = @"HandBrakeCLI.exe";
                                    //handBrake.StartInfo.Arguments = "-t " + t.TitleNumber.ToString() + " -i \"" + d.FullName + "\" -o \"" + fileName + "\" --preset=\"iPhone & iPod Touch\"";
                                    handBrake.StartInfo.Arguments = " -i \"" + d.FullName + "\" -t " + t.TitleNumber.ToString() + " -o \"" + fileName + "\" -f mp4 --strict-anamorphic  --detelecine --decomb -e x264 -q 20 -a 1 -E faac -6 auto -R 48 -B 160 -D 0.0 -x b-adapt=2:rc-lookahead=50:me=umh:direct=auto:merange=64:analyse=all -v 1";
                                    handBrake.StartInfo.RedirectStandardOutput = false;
                                    handBrake.StartInfo.UseShellExecute = false;
                                    handBrake.Start();
                                    handBrake.WaitForExit();
                                }
                                episode++;
                            }

                        previousSeason = season;
                    }
                }
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public class Sorter : IComparer<DirectoryInfo>
        {
            public Regex R;

            public int Compare(DirectoryInfo x, DirectoryInfo y)
            {
                Match m1 = R.Match(x.Name);
                Match m2 = R.Match(y.Name);

                int season1 = int.Parse(m1.Groups["major"].Value);
                int volume1 = int.Parse(m1.Groups["minor"].Value);

                int season2 = int.Parse(m2.Groups["major"].Value);
                int volume2 = int.Parse(m2.Groups["minor"].Value);

                if (season1 > season2)
                    return 1;
                else if (season1 < season2)
                    return -1;
                else if (volume1 > volume2)
                    return 1;
                else if (volume1 < volume2)
                    return -1;
                else
                    return 0;
            }
        }


        static int Sg1Sort(DirectoryInfo d1, DirectoryInfo d2)
        {


            Match m1 = sg1.Match(d1.Name);
            Match m2 = sg1.Match(d2.Name);

            int season1 = int.Parse(m1.Groups[2].Value);
            int volume1 = int.Parse(m1.Groups[1].Value);

            int season2 = int.Parse(m2.Groups[2].Value);
            int volume2 = int.Parse(m2.Groups[1].Value);

            if (season1 > season2)
                return 1;
            else if (season1 < season2)
                return -1;
            else if (volume1 > volume2)
                return 1;
            else if (volume1 < volume2)
                return -1;
            else
                return 0;
        }

        static void scanProcess(object state, string sourcePath)
        {
            try
            {
                string handbrakeCLIPath = "HandBrakeCLI.exe";
                string logDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\HandBrake\\logs";
                string dvdInfoPath = Path.Combine(logDir, "last_scan_log.txt");

                // Make we don't pick up a stale last_encode_log.txt (and that we have rights to the file)
                if (File.Exists(dvdInfoPath))
                    File.Delete(dvdInfoPath);

                String dvdnav = string.Empty;
                /* if (Properties.Settings.Default.noDvdNav)
                    dvdnav = " --no-dvdnav"; */
                string strCmdLine = String.Format(@"cmd /c """"{0}"" -i ""{1}"" -t0 {2} -v >""{3}"" 2>&1""", handbrakeCLIPath, sourcePath, dvdnav, dvdInfoPath);

                ProcessStartInfo hbParseDvd = new ProcessStartInfo("CMD.exe", strCmdLine) /* { WindowStyle = ProcessWindowStyle.Hidden } */;


                Boolean cleanExit = true;
                using (hbproc = Process.Start(hbParseDvd))
                {
                    Process[] before = Process.GetProcesses(); // Get a list of running processes before starting.
                    scanProcessID = getCliProcess(before);
                    hbproc.WaitForExit();
                    if (hbproc.ExitCode != 0)
                        cleanExit = false;
                }

                if (cleanExit) // If 0 exit code, CLI exited cleanly.
                {
                    if (!File.Exists(dvdInfoPath))
                        throw new Exception("Unable to retrieve the DVD Info. last_scan_log.txt is missing. \nExpected location of last_scan_log.txt: \n"
                                            + dvdInfoPath);

                    using (StreamReader sr = new StreamReader(dvdInfoPath))
                    {
                        thisDVD = DVD.Parse(sr);
                    }
                }
            }
            catch (Exception exc)
            {
                //MessageBox.Show("frmMain.cs - scanProcess() " + exc, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static int getCliProcess(Process[] before)
        {
            // This is a bit of a cludge. Maybe someone has a better idea on how to impliment this.
            // Since we used CMD to start HandBrakeCLI, we don't get the process ID from hbProc.
            // Instead we take the processes before and after, and get the ID of HandBrakeCLI.exe
            // avoiding any previous instances of HandBrakeCLI.exe in before.
            // Kill the current process.

            DateTime startTime = DateTime.Now;
            TimeSpan duration;

            Process[] hbProcesses = Process.GetProcessesByName("HandBrakeCLI");
            while (hbProcesses.Length == 0)
            {
                hbProcesses = Process.GetProcessesByName("HandBrakeCLI");
                duration = DateTime.Now - startTime;
                if (duration.Seconds > 5 && hbProcesses.Length == 0) // Make sure we don't wait forever if the process doesn't start
                    return -1;
            }

            Process hbProcess = null;
            foreach (Process process in hbProcesses)
            {
                Boolean found = false;
                // Check if the current CLI instance was running before we started the current one
                foreach (Process bprocess in before)
                {
                    if (process.Id == bprocess.Id)
                        found = true;
                }

                // If it wasn't running before, we found the process we want.
                if (!found)
                {
                    hbProcess = process;
                    break;
                }
            }
            if (hbProcess != null)
                return hbProcess.Id;

            return -1;
        }
    }
}
