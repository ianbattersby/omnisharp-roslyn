using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.IO;

namespace OmniSharp.MSBuild
{
    internal class ProjectAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
            throw new NotImplementedException();
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return Assembly.Load(File.ReadAllBytes(fullPath));
        }
    }
}