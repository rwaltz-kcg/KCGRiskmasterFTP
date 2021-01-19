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
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using System.Configuration;

/* KCGRiskmasterFTP is responsible for FTPing files to/from Riskmaster in the Cloud.
 * The application is responsible for HR Interface and Payment routines
 */
namespace KCGRiskmasterFTP
{
    /* the Options class validates command line arguments and assigns them to class variables. 
     * The values of the commandline arguments can be retrieved from an instance of this class
     */
    class Options
    {
        [Option('c', "Config", Required = true,
        HelpText = "Filepath to the application settings configuration file.")]
        public string appSettingsFilepath { get; set; }

        [Option('o', "OrbitImportFTP", Required = false,
          HelpText = "Run the OrbitImportFTP Function.")]
        public bool orbitImportFTP { get; set; }

        [Option('h', "HRInterfaceRiskmasterFTP", Required = false,
          HelpText = "Run the HRInterfaceRiskmasterFTP Function.")]
        public bool hrInterfaceRiskmasterFTP { get; set; }

    }
    /* the Progam class contains the main method to begin execution of the application */
    class Program
    {
        public static ExecutableFunctions executableFunction;
        public static string appSetttingsFilePath;
        public static System.Configuration.AppSettingsSection appSettings;
        //public static Parser commandLineParser;

        /* Main function begins the execution of the application */
        static void Main(string[] args)
        {
 
 
            try
            {
                /* Parse the command line options */

                Options options = new Options();

                Parser commandLineParser = new Parser();
                commandLineParser.FormatCommandLine(options);
                /* The static function RunOptions is called after parsing the command line arguments */
                ParserResult<Options> parserResult = commandLineParser.ParseArguments<Options>(args).WithParsed(RunOptions);
                /* The static functino RunOptions sets the static class property executableFunction . 
                 * If the property is not set, then display valid arguments to console 
                 */
                if (executableFunction == ExecutableFunctions.None)
                {
                    Console.WriteLine(HelpText.AutoBuild(parserResult, _ => _, _ => _));
                }

                /* 
                 * Load the settings file for the application
                 * The location of the settings file is set by a command line argument
                 */
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", appSetttingsFilePath);
                ConfigurationManager.RefreshSection("appSettings");
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                /* appSettings is a static class property */
                appSettings = (System.Configuration.AppSettingsSection)config.GetSection("appSettings");


                /* WinSCPRunner contains the FTP methods for getting/putting files to Riskmaster */
                WinSCPRunner winSCPRunner = new WinSCPRunner();
                if (winSCPRunner != null) {
                    winSCPRunner.runWinSCP();
                }
            } catch (Exception ex)
            {
                var st = new System.Diagnostics.StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();


                Console.WriteLine(line + " : " + ex.ToString());
                Console.WriteLine(st.ToString());
            }
        }

        static void RunOptions(Options options)
        {
            Program.appSetttingsFilePath = options.appSettingsFilepath;
            if (!options.hrInterfaceRiskmasterFTP && options.orbitImportFTP)
            {
                Program.executableFunction = ExecutableFunctions.OrbitImportFTP;
            } else if (!options.orbitImportFTP && options.hrInterfaceRiskmasterFTP)
            {
                Program.executableFunction = ExecutableFunctions.HRInterfaceRiskmasterFTP;
            }

            Program.appSetttingsFilePath = options.appSettingsFilepath;

        }
    }


}
