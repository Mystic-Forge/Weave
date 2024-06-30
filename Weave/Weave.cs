using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Weave;


public static class Weave {
    /// <summary>
    /// Creates a new WeaveLibrary pre-loaded with all WeaveFunctions in the calling assembly.
    /// </summary>
    /// <returns></returns>
    public static WeaveLibrary CreateLibrary() {
        var library = new WeaveLibrary(null, "");

        foreach (var weaveFunction in Assembly.GetCallingAssembly()
                     .GetTypes()
                     .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                     .Where(t => t.GetCustomAttribute<WeaveFunctionAttribute>() != null)) {
            var attribute = weaveFunction.GetCustomAttribute<WeaveFunctionAttribute>()!;
            var name      = attribute.Path.Split('/').Last();
            library.Set(attribute.Path, new WeaveFunctionInfo(name, weaveFunction));
        }

        foreach (var type in Assembly.GetCallingAssembly().GetTypes()) {
            var attribute = type.GetCustomAttribute<WeaveTypeAttribute>();

            if (attribute is not null) library.Set(attribute.Path, new WeaveType(type, attribute.Path.Split('/').Last()));
        }

        return library;
    }

    public static void StartTest() {
        var globalLibrary = CreateLibrary();
        globalLibrary.IndexDirectory("../../../weave_scripts/");
        globalLibrary.Compile();

        
        var startEvent = globalLibrary.GetFirst<WeaveEventInfo>("test/start");
        // var memory    = globalLibrary.GetFirst<WeaveMemoryInfo>("test/penis");

        foreach (var f in globalLibrary.Get<WeaveScriptDefinition>("*")) {
            if(!f.HasListener(startEvent)) continue;
            var instance   = new WeaveInstance(f);
            // var localPenis = f.LocalLibrary.GetFirst<WeaveMemoryInfo>("penis");
            // Console.WriteLine(localPenis == memory);
            // instance.SetMemory(localPenis, 5);
            // instance.SetMemory(memory, 5);
            instance.Invoke(startEvent);
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Interface)]
public class WeaveTypeAttribute : Attribute {
    public string Path { get; }

    public WeaveTypeAttribute(string path) => Path = path;
}