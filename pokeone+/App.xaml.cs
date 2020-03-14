using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Name { get; private set; }
        public static string Version { get; private set; }
        public static string Author { get; private set; }
        public static string Description { get; private set; }
        public static bool IsBeta { get; private set; } = true;

        public static void InitializeVersion()
        {
            Assembly assembly = typeof(App).Assembly;
            AssemblyName assemblyName = assembly.GetName();
            Name = assemblyName.Name;
            Version = IsBeta ? Version = assemblyName.Version.ToString(3) + "-beta1" : assemblyName.Version.ToString();
            Author = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false)).Company;
            Description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute), false)).Description;
        }
    }
}
