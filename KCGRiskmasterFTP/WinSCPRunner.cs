/*
Copyright 2020 City of Knoxville

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

using System;
using WinSCP;
using System.Configuration;
using System.Runtime.Serialization.Formatters;
using System.IO;
using System.Globalization;

namespace KCGRiskmasterFTP
{
    class WinSCPRunner
    {
        static String currentDateString = DateTime.Now.ToString("yyyy-MM-dd");
        static String currentDateTimeString = DateTime.Now.ToString("u", CultureInfo.CreateSpecificCulture("en-US"));

        public WinSCPRunner()
        {

        }
        /* 
         * runWinSCP will evaulate which FTP method to run 
         * Establish an FTP connection to Riskmaster
         * Log results to a file
         */

        public void runWinSCP()
        {
            // Get the configuration file.

            Session ftpSession = null;
            String ftpResults = "Failure";
            String logFileName = "";
            switch (Program.executableFunction)
            {

                case ExecutableFunctions.HRInterfaceRiskmasterFTP:
                    try
                    {
                        logFileName = String.Format("HRInterfaceRiskmasterFTP-{0}.log", currentDateString);
                        ftpSession = buildSession(logFileName);
                        ftpResults = runHRInterfaceRiskmasterFTP(ftpSession);
                    }
                    catch (Exception exception)
                    {
                        String loggingDirectoryPath = Program.appSettings.Settings["LoggingDirectoryPath"].Value;
                        char separator = System.IO.Path.DirectorySeparatorChar;
                        String errorLogFilepath = String.Format("{0}{1}HRInterfaceRiskmasterFTP-{2}.err", loggingDirectoryPath, System.IO.Path.DirectorySeparatorChar, currentDateString);

                        String errorMessage = String.Format("{0}: Error Message:{1}\n{2}", currentDateTimeString, exception.Message, exception.StackTrace);
                        StreamWriter errorStream = System.IO.File.AppendText(errorLogFilepath);
                        errorStream.WriteLine(errorMessage);
                        errorStream.Flush();


                    }
                    finally
                    {
                        ftpSession.Dispose();
                    }
                    break;

                case ExecutableFunctions.OrbitImportFTP:
                    try
                    {
                        logFileName = String.Format("OrbitImportFTP-{0}.log", currentDateString);
                        ftpSession = buildSession(logFileName);
                        ftpResults = runOrbitImportFTP(ftpSession);
                    }
                    catch (Exception exception)
                    {
                        String loggingDirectoryPath = Program.appSettings.Settings["LoggingDirectoryPath"].Value;
                        char separator = System.IO.Path.DirectorySeparatorChar;
                        String errorLogFilepath = String.Format("{0}{1}OrbitImportFTP-{2}.err", loggingDirectoryPath, System.IO.Path.DirectorySeparatorChar, currentDateString);

                        String errorMessage = String.Format("{0}: Error Message:{1}\n{2}", currentDateTimeString, exception.ToString(), exception.StackTrace);
                        StreamWriter errorStream = System.IO.File.AppendText(errorLogFilepath);
                        errorStream.WriteLine(errorMessage);
                        errorStream.Flush();
                        errorStream.Close();

                    }
                    finally
                    {
                        if (ftpSession != null) { 
                            ftpSession.Dispose();
                        }
                    }

                    break;


                default:
                    break;

            }
            /* write results of the operation to the log file */
            if (! String.IsNullOrEmpty(logFileName)) { 
                String loggingDirectoryPath = Program.appSettings.Settings["LoggingDirectoryPath"].Value;
                char separator = System.IO.Path.DirectorySeparatorChar;
                String logFilepath = String.Format("{0}{1}{2}", loggingDirectoryPath, System.IO.Path.DirectorySeparatorChar, logFileName);
                StreamWriter logStream = System.IO.File.AppendText(logFilepath);
                logStream.WriteLine(ftpResults);
                logStream.Flush();
                logStream.Close();
            }
        }

        /* create an FTP session based on configurable values */
        private Session buildSession(String fileName)
        {
            Session session = new Session();
            session.DebugLogLevel = 0;
            SessionOptions sessionOptions;
            String[] allSessionKeys = Program.appSettings.Settings.AllKeys;
            if (Array.Exists(allSessionKeys, element => element.Equals("FTPS")) )
            {
                sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    FtpSecure = FtpSecure.Explicit,
                    PortNumber = int.Parse( Program.appSettings.Settings["Port"].Value),
                    HostName = Program.appSettings.Settings["HostName"].Value,
                    UserName = Program.appSettings.Settings["UserName"].Value,
                    Password = Program.appSettings.Settings["Password"].Value,
                    PrivateKeyPassphrase = Program.appSettings.Settings["PrivateKeyPassphrase"].Value,
                    TlsClientCertificatePath = Program.appSettings.Settings["SshPrivateKeyPath"].Value,
    
                    TlsHostCertificateFingerprint = Program.appSettings.Settings["SshHostKeyFingerprint"].Value,


                };
                // sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate = true;
            } else { 
                sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = Program.appSettings.Settings["HostName"].Value,
                    UserName = Program.appSettings.Settings["UserName"].Value,
                    PrivateKeyPassphrase = Program.appSettings.Settings["PrivateKeyPassphrase"].Value,
                    SshHostKeyFingerprint = Program.appSettings.Settings["SshHostKeyFingerprint"].Value,
                    SshPrivateKeyPath = Program.appSettings.Settings["SshPrivateKeyPath"].Value
                };
            }
            String loggingDirectoryPath = Program.appSettings.Settings["LoggingDirectoryPath"].Value;
            char separator = System.IO.Path.DirectorySeparatorChar;
            String logFilepath = String.Format("{0}{1}{2}", loggingDirectoryPath, System.IO.Path.DirectorySeparatorChar, fileName);
            session.SessionLogPath = logFilepath;
            session.DisableVersionCheck = true;

            session.Open(sessionOptions);

            return session;


        }
        /* upload a file from a shared directory to Riskmaster for the HR Interface based on configurable values */
        private string runHRInterfaceRiskmasterFTP(Session ftpSession)
        {
            System.IO.StringWriter strWriter = new System.IO.StringWriter();
            String sourceDirectory = Program.appSettings.Settings["RiskMasterLocalDirectory"].Value;
            String archiveDirectory = Program.appSettings.Settings["RiskMasterArchiveDirectory"].Value;
            String riskMasterRemoteDirectory = Program.appSettings.Settings["RiskMasterRemoteDirectory"].Value;
            String hrInterfaceFilename = Program.appSettings.Settings["HRInterfaceFilename"].Value;
            String fileToFTP = Path.Combine(sourceDirectory, hrInterfaceFilename);

            strWriter.WriteLine("BEGIN runHRInterfaceRiskmasterFTP " + currentDateTimeString);
            if (File.Exists(fileToFTP))
            {

                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = WinSCP.TransferMode.Binary;
                transferOptions.OverwriteMode = WinSCP.OverwriteMode.Overwrite;
                transferOptions.PreserveTimestamp = true;

                // make a copy of the HRInterface file for archival and auditing purposes
                string hrInterfaceArchiveFilename = hrInterfaceFilename + "-" + currentDateString;
                string hrInterfaceArchiveFile = Path.Combine(archiveDirectory, hrInterfaceArchiveFilename);
                File.Copy(fileToFTP, hrInterfaceArchiveFile, true);
                strWriter.WriteLine("Copied " + hrInterfaceArchiveFilename + " to " + archiveDirectory);
                /* capture console/standard out from the ftp session to a string to be written after all is done */

                Console.SetOut(strWriter);
                ftpSession.PutFileToDirectory(fileToFTP, riskMasterRemoteDirectory, false, transferOptions);

                strWriter.WriteLine("END runHRInterfaceRiskmasterFTP");
            }
            else
            {
                strWriter.WriteLine("END runHRInterfaceRiskmasterFTP " + fileToFTP + " file does not exist.");
            }
            return strWriter.ToString();
        }
        /* download files to a Shared Directory from  Riskmaster for the Riskmaster Payments to Orbit interface based on configurable values */
        private string runOrbitImportFTP(Session ftpSession)
        {
            System.IO.StringWriter strWriter = new System.IO.StringWriter();
            strWriter.WriteLine("BEGIN runOrbitImportFTP " + currentDateTimeString);
            TransferOptions transferOptions = new TransferOptions();
            transferOptions.TransferMode = WinSCP.TransferMode.Binary;
	        transferOptions.OverwriteMode = WinSCP.OverwriteMode.Overwrite;
            transferOptions.FileMask = "*.csv";
            transferOptions.PreserveTimestamp = true;

            String orbitImportRemoteDirectory = Program.appSettings.Settings["OrbitImportRemoteDirectory"].Value;
            String orbitImportLocalDirectory = Program.appSettings.Settings["OrbitImportLocalDirectory"].Value;

            /* capture console/standard out from the ftp session to a string to be written after all is done */

            Console.SetOut(strWriter);
            ftpSession.GetFilesToDirectory(orbitImportRemoteDirectory, orbitImportLocalDirectory, transferOptions.FileMask, true, transferOptions);

            // validate and standardize any downloaded filenames
            string[] riskmasterFileList = Directory.GetFiles(orbitImportLocalDirectory, "*.csv");
            strWriter.WriteLine("runOrbitImportFTP found in  " + orbitImportLocalDirectory + " " + string.Join(", ", riskmasterFileList));
            foreach (string riskmasterFile in riskmasterFileList)
            {
                // Make certain that the files conform to a pattern of P(d|f|b)\d5_\d7.csv
                if ( !(riskmasterFile.Contains("_")) && riskmasterFile.EndsWith(".csv"))
                {
                    // rename the file to 
                    string basefileName = Path.GetFileNameWithoutExtension(riskmasterFile);
                    string newFilename = basefileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
                    string newRiskmasterFile = Path.Combine(orbitImportLocalDirectory, newFilename);
                    File.Move(riskmasterFile, newRiskmasterFile);
                }
               

            }

            strWriter.WriteLine("BEGIN runOrbitImportFTP ");
            return strWriter.ToString();
        }
    }
}
