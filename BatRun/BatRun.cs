﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Management;

class Run
{
    public string exePath { get; set; }
    public string inputFile { get; set; }
    public string outputDir { get; set; }
    public string format { get; set; }
    public int timeout { get; set; }
    public string replace { get; set; }
    public int start { get; set; }
    public string fullpath = null;
    public string savepath = null;
    public int index = 0;
    public int all = 0;
    public Process cur_p = null;
    public Thread cur_t = null;
    
    public void RunSingle()
    {
        string cmd = exePath + " -i " + fullpath + " -o " + outputDir + " -r " + replace + " -f " + format;
        try
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cur_p = p;
            p.Start();
            p.StandardInput.WriteLine(cmd + "&exit");
            p.StandardInput.AutoFlush = true;
            //string outStr = p.StandardOutput.ReadToEnd();
            //Console.WriteLine(outStr);
            p.WaitForExit();
            p.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void RunBat()
    {
        FileInfo[] files= {null};
        if (Directory.Exists(inputFile))
        {
            DirectoryInfo root = new DirectoryInfo(inputFile);
            files = root.GetFiles();
            Console.WriteLine("start with: {0}\n",start);
        }
        else if (File.Exists(inputFile))
        {
            FileInfo single =  new FileInfo(inputFile);
            files[0] = single;
        }
        else
        {
            Console.WriteLine("Can not open: {0}", inputFile);
            return;
        }
        all = files.Length;
        for (int i = start; i < files.Length; i++)
        {
            try
            {
                index = i;
                fullpath = files[i].FullName;
                
                //Console.WriteLine(fullpath);
                savepath  = outputDir + '\\' + Path.GetFileNameWithoutExtension(fullpath) + "." + format;
                //Console.WriteLine(savepath);

                Thread t = new Thread(RunSingle);
                t.Start();

                Thread timer = new Thread(TimerElapsed);
                timer.Start();
                timer.Join();

                //if (cur_t != null)
                //{
                //    try
                //    {
                //        //Console.WriteLine("kill thread: {0}", t.ManagedThreadId);
                //        cur_t.Abort();
                //    }
                //    catch (Exception excep)
                //    {
                //        //Console.WriteLine(excep);
                //    }
                //}
                

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public void DeleteFiles(string dir)
    {
        DirectoryInfo di = new DirectoryInfo(dir);
        if (di.GetDirectories().Length == 0 && di.GetFiles().Length == 0) return;
        foreach (DirectoryInfo d in di.GetDirectories())
            DeleteFiles(d.FullName);
        foreach (FileInfo fi in di.GetFiles())
            fi.Delete();
        di.Delete();
    }

    public void KillProcessAndChildren(int pid)
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
        ManagementObjectCollection moc = searcher.Get();
        foreach (ManagementObject mo in moc)
        {
            KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
        }
        try
        {
            Process proc = Process.GetProcessById(pid);
            //Console.WriteLine(pid);
            proc.Kill();
        }
        catch (ArgumentException)
        {
            /* process already exited */
        }
    }


    public void KillAcrobat()
    {
        if (cur_p != null)
        {
            try
            {
                //Console.WriteLine("kill process: {0}", p.Id);
                KillProcessAndChildren(cur_p.Id);
            }
            catch (Exception excep)
            {
                //Console.WriteLine(excep);
            }
        }

        Process[] P = Process.GetProcessesByName("Acrobat");
        foreach (Process p in P)
        {
            try
            {
                KillProcessAndChildren(p.Id);
            }
            catch
            {

            }
        }
    }

    private void TimerElapsed()
    {


        int total = timeout;
        while (total > 0 && !File.Exists(savepath))
        {
            //Console.WriteLine(total);
            Thread.Sleep(1000);
            total -= 1000;
        }

        if (File.Exists(savepath))
        {
            Console.WriteLine("{0}/{1} sucessfully!\n",index,all);
            if (index % 10 == 0)
            {
                //Console.WriteLine("kill acrobat!\n");
                KillAcrobat();
            }
        }
        else
        {
            Console.WriteLine("{0}/{1} failed!\n\n", index, all);
            string tmp_name = outputDir + '\\' + Path.GetFileNameWithoutExtension(fullpath) + ".skip";
            FileStream fw = new FileStream(tmp_name, FileMode.Create, FileAccess.Write);
            fw.Close();
            KillAcrobat();
            //Thread.Sleep(2000);

        }
        if (Directory.Exists(outputDir + '\\' + Path.GetFileNameWithoutExtension(fullpath)))
        {
            DeleteFiles(outputDir + '\\' + Path.GetFileNameWithoutExtension(fullpath));
        }
    }

    class BatRun
    {

        static void Main(string[] args)
        {
            string exePath = null;
            string inputFile = null;
            string outputDir = Application.StartupPath;
            string format = "html";
            int timeout = 20000;
            string replace = "false";

            // get args
            var arguments = CommandLineArgumentParser.Parse(args);
            if (!arguments.Has("-i") || !arguments.Has("-e"))
            {
                Console.WriteLine("Usage: RunBat.exe \n" +
                    "-e exe file path\n" +
                    "-i inputfile \n" +
                    "-o outputdir\tdefault: run dictionary)\n" +
                    "-r replace exist file\t(default:false)\n" +
                    "-t timeout\t dedault: 20000ms\n" +
                    "-f format\tdefault: html\n\t\t" +
                    "Support: xml,txt,doc,docx,\n\t\t" +
                    "ps,jpeg,jpe,jpg,\n\t\t" +
                    "jpf,jpx,j2k,j2c,jpc,rtf,\n\t\t" +
                    "accesstext,tif,tiff)\n" +
                    "-h help\n");
                Environment.Exit(0);
            }
            else
            {
                inputFile = arguments.Get("-i").Next;
                exePath = arguments.Get("-e").Next;
            }
            if (arguments.Has("-o"))
            {
                outputDir = arguments.Get("-o").Next;
            }
            if (arguments.Has("-f"))
            {
                format = arguments.Get("-f").Next;
            }
            if (arguments.Has("-r"))
            {
                if ("true" == arguments.Get("-r").Next)
                {
                    replace = "true";
                }
            }
            if (arguments.Has("-t"))
            {
                try
                {
                    timeout = Convert.ToInt32(arguments.Get("-t").Next);
                }
                catch
                {
                    timeout = 20000;
                }
            }
            if (arguments.Has("-h"))
            {
                Console.WriteLine("Usage: RunBat.exe \n" +
                    "-e exe file path\n" +
                    "-i inputfile or dictinary \n" +
                    "-o outputdir\tdefault: run dictionary)\n" +
                    "-r replace exist file\t(default:false)\n" +
                    "-f format\tdefault: html\n\t\t" +
                    "support: xml,txt,doc,docx,\n\t\t" +
                    "ps,jpeg,jpe,jpg,\n\t\t" +
                    "jpf,jpx,j2k,j2c,jpc,rtf,\n\t\t" +
                    "accesstext,tif,tiff)\n" +
                    "-h help\n");
                Environment.Exit(0);
            }
            DirectoryInfo tmp = new DirectoryInfo(outputDir);
            int start = tmp.GetFiles().Length;

            Run run = new Run
            {
                exePath = exePath,
                inputFile = inputFile,
                outputDir = outputDir,
                format = format,
                timeout = timeout,
                replace = replace,
                fullpath = inputFile,
                start = start
            };

            run.RunBat();
           
        }
    }
}

