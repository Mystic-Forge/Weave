using System;
using System.Collections.Generic;

using Antlr4.Runtime.Tree;


namespace Weave;


public class WeaveLibrary {
    private readonly Dictionary<string, WeaveLibrary>      _children = new();
    private readonly Dictionary<string, WeaveLibraryEntry> _entries   = new();
    
    public WeaveLibraryEntry this[string path] {
        get {
            var pathIndex = path.IndexOf('/');
            if (pathIndex == -1) return _entries[path];
            return _children[path.Substring(0, pathIndex)][path.Substring(pathIndex + 1)];
        }
        set {
            var pathIndex = path.IndexOf('/');
            if (pathIndex == -1) {
                _entries[path] = value;
                return;
            }
            GetOrCreateChild(path.Substring(0, pathIndex))[path.Substring(pathIndex + 1)] = value;
        }
    }
    
    public bool TryGet(string path, out WeaveLibraryEntry entry) {
        var pathIndex = path.IndexOf('/');
        if (pathIndex == -1) return _entries.TryGetValue(path, out entry);
        if (!_children.TryGetValue(path.Substring(0, pathIndex), out var child)) {
            entry = null!;
            return false;
        }
        return child.TryGet(path.Substring(pathIndex + 1), out entry);
    }
    
    private WeaveLibrary GetOrCreateChild(string name) {
        if (_children.TryGetValue(name, out var child)) return child;
        return _children[name] = new();
    }
}

public abstract class WeaveLibraryEntry {
}

public class WeaveEventInfo : WeaveLibraryEntry {
    public string Name { get; }

    public Type[] ParameterTypes { get; }

    public WeaveEventInfo(string name, params Type[] parameterTypes) {
        Name           = name;
        ParameterTypes = parameterTypes;
    }
}