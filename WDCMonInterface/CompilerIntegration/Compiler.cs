using System.Diagnostics;

namespace WDCMonInterface.CompilerIntegration
{
    class Compiler
    {
        public static string? CompileAssembly(string FileName, string? ConfigurationFile = null)
        {
            string? parentDirectory = Path.GetDirectoryName(FileName);
            if (parentDirectory == null) return null;
            string? buildDirectory = Path.Combine(parentDirectory, "obj");
            string? binDirectory = Path.Combine(parentDirectory, "bin");
            if (buildDirectory == null || binDirectory == null) return null;

            Directory.CreateDirectory(buildDirectory);
            Directory.CreateDirectory(binDirectory);

            var di = new DirectoryInfo(buildDirectory);
            foreach (FileInfo file in di.GetFiles()) file.Delete();
            foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);

            string programName = Path.GetFileNameWithoutExtension(FileName);

            // Compiler CA65 <filename> with -g -o outfile

            string ObjectFileName = Path.Combine(buildDirectory, $"{programName}.o");

            var compilerExitCode = Program.RunProcess("CA65", $"{FileName} -g -o {ObjectFileName}");
            if (compilerExitCode != 0)
            {
                Program.Stderr($"Could not compile {FileName}: CA65 returned exit code {compilerExitCode}.");
                throw new Exception($"CA65 returned: {compilerExitCode}");
            }

            string dbgFileName = Path.Combine(binDirectory, $"{programName}.dbg");
            string binaryFileName = Path.Combine(binDirectory, $"{programName}.bin");

            string configFileString = "-t none";
            if (!string.IsNullOrWhiteSpace(ConfigurationFile))
            {
                configFileString = $"-C {ConfigurationFile}";
            }

            var linkerExitCode = Program.RunProcess("LD65", $"{ObjectFileName} {configFileString} -o {binaryFileName} --dbgfile {dbgFileName}");
            if (linkerExitCode != 0)
            {
                Program.Stderr($"Could not compile {FileName}: LD65 returned exit code {linkerExitCode}.");
                throw new Exception($"LD65 returned: {linkerExitCode}");
            }

            return dbgFileName;
        }
    }

}