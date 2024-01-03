using System;
using System.Collections.Generic;

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
    
    public void AddEvent(string name, WeaveEventInfo eventInfo) => _entries[name] = eventInfo;
    
    public WeaveLibrary GetChild(string name) => _children[name];
    
    private WeaveLibrary GetOrCreateChild(string name) {
        if (_children.TryGetValue(name, out var child)) return child;
        return _children[name] = new();
    }
}

public abstract class WeaveLibraryEntry {
}

public class WeaveFileDefinition : WeaveLibraryEntry {
    private readonly Dictionary<WeaveEventInfo, Delegate> _listeners = new();

    public void AddListener(WeaveEventInfo listenerInfo, Delegate listener) => _listeners[listenerInfo] = listener;

    public void Invoke(WeaveInstance instance, WeaveEventInfo weaveEventInfo) {
        if (_listeners.TryGetValue(weaveEventInfo, out var listener)) listener.DynamicInvoke();
    }

    public void Invoke<T>(WeaveInstance instance, WeaveEventInfo<T> weaveEventInfo, T arg) {
        if (_listeners.TryGetValue(weaveEventInfo, out var listener)) listener.DynamicInvoke(arg);
    }
    
    public void Invoke<T1, T2>(WeaveInstance instance, WeaveEventInfo<T1, T2> weaveEventInfo, T1 arg1, T2 arg2) {
        if (_listeners.TryGetValue(weaveEventInfo, out var listener)) listener.DynamicInvoke(arg1, arg2);
    }

    public WeaveInstance CreateInstance() => new(this);
}

public class WeaveEventInfo : WeaveLibraryEntry {
    public string Name { get; }

    public Type[] ParameterTypes { get; }

    public WeaveEventInfo(string name, params Type[] parameterTypes) {
        Name           = name;
        ParameterTypes = parameterTypes;
    }
}

public class WeaveEventInfo<T> : WeaveEventInfo {
    public WeaveEventInfo(string name) : base(name, typeof(T)) { }
}

public class WeaveEventInfo<T1, T2> : WeaveEventInfo {
    public WeaveEventInfo(string name) : base(name, typeof(T1), typeof(T2)) { }
}