using System;
using System.Linq;
using System.Reflection;


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

        var startEvent = globalLibrary.GetFirst<WeaveEventInfo>("a/builtin/start");

        foreach (var f in globalLibrary.Get<WeaveScriptDefinition>("*")) {
            // Console.WriteLine($"Running {f}");
            var instance = new WeaveInstance(f);
            instance.Invoke(startEvent, 5, 2);
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Interface)]
public class WeaveTypeAttribute : Attribute {
    public string Path { get; }

    public WeaveTypeAttribute(string path) => Path = path;
}