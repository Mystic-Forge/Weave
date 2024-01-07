using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Antlr4.Runtime;


namespace Weave;


public class WeaveInstance {
    public WeaveScriptDefinition ScriptDefinition { get; private set; }

    private readonly Dictionary<WeaveMemoryInfo, object> _memories = new();

    public WeaveInstance(WeaveScriptDefinition  scriptDefinition) => ScriptDefinition = scriptDefinition;

    public void SetScript(WeaveScriptDefinition scriptDefinition) => ScriptDefinition = scriptDefinition;

    public void Invoke(WeaveEventInfo weaveEventInfo, params object[] arguments) => ScriptDefinition.Invoke(this, weaveEventInfo, arguments);

    public object? GetMemory(WeaveMemoryInfo memoryInfo) => _memories.TryGetValue(memoryInfo, out var value) ? value : null;

    public void SetMemory(WeaveMemoryInfo memoryInfo, object value) => _memories[memoryInfo] = value;
}

public class WeaveScriptDefinition : WeaveLibraryEntry {
    private readonly  Dictionary<WeaveEventInfo, Delegate> _listeners = new();
    internal readonly WeaveLibrary                         LocalLibrary;

    internal readonly WeaveParser.StartContext Tree;

    internal Type SelfType = typeof(WeaveInstance);

    internal WeaveScriptDefinition(string path) {
        LocalLibrary = new(null, "");

        Name = Path.GetFileNameWithoutExtension(path);

        var stream = new AntlrInputStream(new FileStream(path, FileMode.Open));
        var lexer  = new WeaveLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new WeaveParser(tokens);
        parser.BuildParseTree = true;
        Tree                  = parser.start();
    }

    public void AddListener(WeaveEventInfo listenerInfo, Delegate listener) => _listeners[listenerInfo] = listener;

    public void Invoke(WeaveInstance instance, WeaveEventInfo weaveEventInfo, params object[] arguments) {
        if (_listeners.TryGetValue(weaveEventInfo, out var listener)) listener.DynamicInvoke(arguments.Prepend(instance).ToArray());
    }
}