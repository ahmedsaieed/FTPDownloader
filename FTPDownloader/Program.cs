using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web;
using System.Threading;
using System.Configuration;
using NLog;

namespace FTPDownloader
{

    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static string ftpServerIP;
        public static string ftpUserID;
        public static string ftpPassword;
        public static string localDestnDir;
        public static string gcodeDir;
        public static Int32 refreshRate;

    


        static void Main(string[] args)
        {
            logger.Info("Starting Periodic FTP Downloader");

            Console.WriteLine("JcDecaux FTP File Downloader");
            Console.WriteLine("Ver 1.0 - 22 Jan 2015");
            Console.WriteLine("asaieed@fractal.ae - Fractal Systems");
            Console.WriteLine("");
            Console.WriteLine("WARNING:");
            Console.WriteLine("Closing this window will stop the automatic download of new content.");

            while (true)
            {
                logger.Trace("Reading configuration.");
                ftpServerIP = System.Configuration.ConfigurationSettings.AppSettings["ftpServerUri"].ToString();
                ftpUserID = System.Configuration.ConfigurationSettings.AppSettings["ftpUserID"].ToString();
                ftpPassword = System.Configuration.ConfigurationSettings.AppSettings["ftpPass"].ToString();
                localDestnDir = System.Configuration.ConfigurationSettings.AppSettings["localDestnDir"].ToString();
                gcodeDir = System.Configuration.ConfigurationSettings.AppSettings["gcodeDir"].ToString();
                refreshRate = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["refreshRate"]);

                string[] files = GetFileList();
                if (files != null)
                {
                    foreach (string file in files)
                    {
                        logger.Info("Downloading " + file);
                        Download(file);
                    }
                }
                else
                {
                    logger.Error("Error connecting to FTP server or no files found!");
                }
                logger.Trace("Next FTP server poll after" + refreshRate + "millsecs");
                logger.Info("Sleeping...");
                Thread.Sleep(refreshRate);
            }
        }

        private static void Download(string file)
        {
            try
            {
                string uri = "ftp://" + ftpServerIP + "/" + file;
                logger.Trace("Connecting to " + "ftp://" + ftpServerIP + "/" + file);
                Uri serverUri = new Uri(uri);
                if (serverUri.Scheme != Uri.UriSchemeFtp)
                {
                    logger.Error("Illegal FTP address.");
                    return;
                }
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpServerIP  + "/" + file));
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                reqFTP.Proxy = null;
                reqFTP.UsePassive = false;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream responseStream = response.GetResponseStream();
                string dir = "";
                if (file.EndsWith(".nc"))
                {
                    logger.Trace("GCode file found: " + file);
                    dir = gcodeDir;
                }
                else
                {
                    logger.Trace("File found: " + file);
                    dir = localDestnDir;
                }
                FileStream writeStream = new FileStream(dir + "\\" + file, FileMode.Create);
                logger.Trace("Downloading to " + dir + "\\" + file);
                int Length = 2048;
                Byte[] buffer = new Byte[Length];
                int bytesRead = responseStream.Read(buffer, 0, Length);
                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = responseStream.Read(buffer, 0, Length);
                }
                writeStream.Close();
                response.Close();
                logger.Trace("Download complete!");
            }
            catch (WebException wEx)
            {
                logger.Error(wEx.Message, "Download Error");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, "Download Error");
            }
        }

        private static string[] GetFileList()
        {
            string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            WebResponse response = null;
            StreamReader reader = null;
            try
            {
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpServerIP + "/"));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                reqFTP.Proxy = null;
                reqFTP.KeepAlive = false;
                reqFTP.UsePassive = false;
                response = reqFTP.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                // to remove the trailing '\n'
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                logger.Error("Unknown Error." + ex);
                if (reader != null)
                {
                    reader.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
                downloadFiles = null;
                return downloadFiles;
            }
        }
    }
}