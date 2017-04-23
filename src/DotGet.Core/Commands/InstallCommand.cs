using System;
using System.IO;
using System.Runtime.InteropServices;

using DotGet.Core.Configuration;
using DotGet.Core.Logging;
using DotGet.Core.Resolvers;

namespace DotGet.Core.Commands
{
    public class InstallCommand
    {
        private string _tool;
        private CommandOptions _options;
        private ILogger _logger;

        public InstallCommand(string tool, CommandOptions options, ILogger logger)
        {
            _tool = tool;
            _options = options;
            _logger = logger;
        }

        private ResolverOptions BuildResolverOptions()
        {
            ResolverOptions resolverOptions = new ResolverOptions();
            foreach (var option in _options)
                resolverOptions.Add(option.Key, option.Value);
            
            return resolverOptions;
        }

        private string GetBinContents(string dllPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"dotnet {dllPath} %*";

            return $"#!/usr/bin/env bash \ndotnet {dllPath} \"$@\"";
        }

        private string GetBinFilename(string dllPath)
        {
            string filename = Path.GetFileNameWithoutExtension(dllPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return filename + ".cmd";

            return filename;
        }

        private string GetEtcContents()
        {
            string contents = $"tool=:={_tool}\n";
            foreach (var option in _options)
                contents += $"{option.Key}=:={option.Value}\n";

            return contents;
        }

        public void Execute()
        {
            Resolver resolver = new ResolverFactory(_tool, BuildResolverOptions(), _logger).GetResolver();
            (bool success, string dllPath) = resolver.Resolve();
            if (!success)
            {
                _logger.LogError($"Failed to install {_tool}!");
                return;
            }

            string globalNugetDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetEnvironmentVariable("USERPROFILE") : Environment.GetEnvironmentVariable("HOME");
            globalNugetDirectory = Path.Combine(globalNugetDirectory, ".nuget");

            string etcDirectory = Path.Combine(globalNugetDirectory, "etc");
            if (!Directory.Exists(etcDirectory))
                Directory.CreateDirectory(etcDirectory);

            string binDirectory = Path.Combine(globalNugetDirectory, "bin");
            if (!Directory.Exists(binDirectory))
                Directory.CreateDirectory(binDirectory);

            string binFileName = GetBinFilename(dllPath);
            File.WriteAllText(Path.Combine(etcDirectory, binFileName), GetEtcContents());
            File.WriteAllText(Path.Combine(binDirectory, binFileName), GetBinContents(dllPath));
            // TODO: make unix bin file executable

            _logger.LogSuccess($"{_tool} successfully installed!");
        }
    }
}