using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using DotGet.Core.Logging;
using DotGet.Core.Resolvers;

namespace DotGet.Core.Commands
{
    public class UpdateCommand : ICommand
    {
        private string _tool;
        private ILogger _logger;

        public UpdateCommand(string tool, ILogger logger)
        {
            _tool = tool;
            _logger = logger;
        }

        public Dictionary<string, string> GetEtc(string path)
        {
            string json = File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        public bool Execute()
        {
            string globalNugetDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetEnvironmentVariable("USERPROFILE") : Environment.GetEnvironmentVariable("HOME");
            globalNugetDirectory = Path.Combine(globalNugetDirectory, ".nuget");

            string etcDirectory = Path.Combine(globalNugetDirectory, "etc");

            string[] etcFiles = Directory.GetFiles(etcDirectory);
            Dictionary<string, string> etc = null;
            string toolEtc = null;

            foreach (string filePath in etcFiles)
            {
                etc = GetEtc(filePath);
                if (etc["tool"] == _tool)
                {
                    toolEtc = filePath;
                    break;
                }
            }

            if (toolEtc == null)
            {
                _logger.LogError($"No tool with name: {_tool}, is installed");
                return false;
            }

            InstallCommand installCommand = new InstallCommand(_tool, _logger);
            installCommand.ResolutionType = ResolutionType.Update;
            return installCommand.Execute();
        }
    }
}