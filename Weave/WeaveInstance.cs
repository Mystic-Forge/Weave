using System;
using System.Collections.Generic;
using System.IO;

using Antlr4.Runtime;


namespace Weave;


public class WeaveInstance {
    public WeaveFileDefinition FileDefinition { get; }

    public WeaveInstance(WeaveFileDefinition fileDefinition) => FileDefinition = fileDefinition;

    public void Invoke(WeaveEventInfo weaveEventInfo, params object[] arguments) => FileDefinition.Invoke(this, weaveEventInfo, arguments);
}

public class WeaveFileDefinition {
    public readonly   string                               Name;
    private readonly  Dictionary<WeaveEventInfo, Delegate> _listeners = new();
    internal readonly WeaveLibrary                         Library;

    internal readonly WeaveParser.StartContext Tree;

    public WeaveFileDefinition(string path) {
        Library = new();

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
        if (_listeners.TryGetValue(weaveEventInfo, out var listener)) listener.DynamicInvoke(arguments);
    }

    public WeaveInstance CreateInstance() => new(this);
}