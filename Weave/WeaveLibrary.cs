using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Antlr4.Runtime.Tree;


namespace Weave;


public class WeaveLibrary {
    private readonly  WeaveLibrary?                         _parent;
    private readonly  Dictionary<string, WeaveLibrary>      _children = new();
    private readonly  Dictionary<string, WeaveLibraryEntry> _entries  = new();
    internal readonly string                                Name;
    internal readonly string                                LibraryPath;

    internal WeaveLibrary(WeaveLibrary? parent, string name) {
        _parent = parent;
        Name    = name;
        var parentPath = parent?.LibraryPath ?? "";
        LibraryPath = parentPath == "" ? name : $"{parentPath}/{name}";
    }

    public IEnumerable<WeaveLibraryEntry> Get(string path) {
        var pathIndex = path.IndexOf('/');

        if (pathIndex == -1)
            return path == "*"
                ? _entries.Values.Concat(_children.SelectMany(child => child.Value.Get("*")))
                : _entries.TryGetValue(path, out var v)
                    ? new[] {
                        v,
                    }
                    : Enumerable.Empty<WeaveLibraryEntry>();

        var nextPart = path.Substring(0, pathIndex);
        if (nextPart == "..") return _parent?.Get(path.Substring(pathIndex + 1)) ?? Enumerable.Empty<WeaveLibraryEntry>();

        if (_children.TryGetValue(nextPart, out var c)) return c.Get(path.Substring(pathIndex + 1));

        throw new($"Library {LibraryPath} does not contain sub-path {path}.");
    }

    public WeaveLibraryEntry GetFirst(string path) => Get(path).First();

    public IEnumerable<T> Get<T>(string path) where T : WeaveLibraryEntry => Get(path).OfType<T>();

    public T GetFirst<T>(string path) where T : WeaveLibraryEntry => Get<T>(path).First();

    public WeaveLibrary GetLibrary(string path) {
        var pathIndex = path.IndexOf('/');

        if (pathIndex == -1)
            return path == "*"
                ? throw new("Cannot get library with wildcard (*)")
                : _children.TryGetValue(path, out var v)
                    ? v
                    : throw new($"Library {path} does not exist");

        var nextPart = path.Substring(0, pathIndex);
        if (nextPart == "..") return _parent?.GetLibrary(path.Substring(pathIndex + 1)) ?? throw new($"Library {path} does not exist");

        return _children[nextPart].GetLibrary(path.Substring(pathIndex + 1));
    }

    public void Set(string path, WeaveLibraryEntry value) {
        Console.WriteLine($"Setting {path} to {value.Name}");
        var pathIndex = path.IndexOf('/');

        if (pathIndex == -1) {
            _entries[path] = value;
            value.Library  = this;
            return;
        }
        

        GetOrCreateChild(path.Substring(0, pathIndex)).Set(path.Substring(pathIndex + 1), value);
    }
    
    /// <summary>
    /// Informs the library that a type exists at the given path. Adding types to a library allows you to use them within Weave scripts.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="type"></param>
    public void AddType(string path, Type type) {
        var name = path.Split('/').Last();
        var weaveType = new WeaveType(type, name);
        Set(path, weaveType);
    }

    public bool TryGetFirst(string path, out WeaveLibraryEntry entry) {
        var results = Get(path).ToArray();

        if (results.Any()) {
            entry = results.First();
            return true;
        }

        entry = null!;
        return false;
    }

    public bool TryGetFirst<T>(string path, out T entry) where T : WeaveLibraryEntry {
        var results = Get<T>(path).ToArray();

        if (results.Any()) {
            entry = results.First();
            return true;
        }

        entry = null!;
        return false;
    }

    private WeaveLibrary GetOrCreateChild(string name) {
        if (_children.TryGetValue(name, out var child)) return child;

        return _children[name] = new(this, name);
    }

    /// <summary>
    /// Indexes all files in a directory to the library with the targetLibraryPath as the root.
    /// The files are not compiled after this operation as you may want to index more directories first.
    /// To compile them, call Compile on this Library.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="targetLibraryPath"></param>
    public void IndexDirectory(string path, string? targetLibraryPath = null) {
        var rootLibraryPath = targetLibraryPath != null ? targetLibraryPath + "/" : "";
        var normalizedPath  = Path.GetFullPath(path);

        foreach (var filePath in Directory.GetFiles(normalizedPath)) {
            var relativePath = rootLibraryPath + filePath.Substring(normalizedPath.Length + 1).Replace("\\", "/");
            relativePath = relativePath.Substring(0, relativePath.Length - 3);
            IndexFile(new(filePath), relativePath);
        }

        foreach (var folderPath in Directory.GetDirectories(normalizedPath)) {
            // Console.WriteLine($"Indexing folder {folderPath}");
            IndexDirectory(folderPath, rootLibraryPath + Path.GetFileName(folderPath));
        }
    }

    private void IndexFile(WeaveScriptDefinition scriptDefinition, string path) {
        Set(path, scriptDefinition);
        var firstPassListener = new WeaveLibraryListener(scriptDefinition, this);
        ParseTreeWalker.Default.Walk(firstPassListener, scriptDefinition.Tree);
    }

    /// <summary>
    /// Compiles all files in the library. You must call this after indexing all files you want to compile and before using the files in the library.
    /// </summary>
    public void Compile() {
        foreach (var file in Get<WeaveScriptDefinition>("*")) CompileFile(file);
    }

    private void CompileFile(WeaveScriptDefinition scriptDefinition) {
        var secondPassListener = new WeaveListener(scriptDefinition, this);
        ParseTreeWalker.Default.Walk(secondPassListener, scriptDefinition.Tree);
        Console.WriteLine($"[Weave] Compiled {scriptDefinition.Name}");
    }
}

public abstract class WeaveLibraryEntry {
    public string Name { get; protected set; } = null!;

    public WeaveLibrary Library { get; internal set; } = null!;
}

public class WeaveEventInfo : WeaveLibraryEntry {
    public Dictionary<string, Type> Parameters { get; }

    public WeaveEventInfo(string name, Dictionary<string, Type> parameters) {
        Name       = name;
        Parameters = parameters;
    }
}

public class WeaveFunctionInfo : WeaveLibraryEntry {
    public Dictionary<string, Type> Parameters { get; }

    public Type? ReturnType { get; }

    public WeaveScriptDefinition? OwnerScript { get; }

    internal WeaveParser.FunctionContext? Tree;

    private Delegate? _compiledExpression;

    internal bool IsCompiled;

    public WeaveFunctionInfo(WeaveScriptDefinition script, WeaveParser.FunctionContext tree, string name, Dictionary<string, Type> parameters, Type? returnType) {
        OwnerScript = script;
        Tree        = tree;
        Name        = name;
        Parameters  = parameters;
        ReturnType  = returnType;
    }

    public WeaveFunctionInfo(string name, MethodInfo method) {
        if (!method.IsStatic) throw new ArgumentException("Weave functions must be static", nameof(method));

        Name = name;

        Parameters = method.GetParameters()
            .Select(p => (p.Name!, p.ParameterType))
            .ToDictionary(p => p.Item1, p => p.Item2);

        ReturnType = method.ReturnType;

        _compiledExpression = method.CreateDelegate(Expression.GetDelegateType(Parameters.Values.Concat(new[] {
                ReturnType,
            })
            .ToArray()));

        IsCompiled = true;
    }

    internal static readonly MethodInfo InvokeMethodInfo = typeof(WeaveFunctionInfo).GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Instance)!;

    internal object Invoke(params object[] arguments) => _compiledExpression!.DynamicInvoke(arguments);

    internal void CompileExpression() {
        if (IsCompiled) return;

        IsCompiled = true;
        var scope = new WeaveListener.Scope(null, null);

        var parameters = Parameters
            .Select(p => {
                var param = Expression.Parameter(p.Value, p.Key);
                scope.AddLocalVariable(p.Key, param);
                return param;
            })
            .ToArray();

        var returnTarget = Expression.Label(ReturnType ?? typeof(void));
        scope.ReturnTarget = returnTarget;

        var body = Expression.Block(
            WeaveListener.ParseTree(OwnerScript, Tree.block(), scope),
            Expression.Label(returnTarget, Expression.Default(ReturnType ?? typeof(void))));

        _compiledExpression = Expression.Lambda(body, parameters).Compile();
    }
}

public class WeaveMemoryInfo : WeaveLibraryEntry {
    public Type Type { get; }

    public WeaveMemoryInfo(string name, Type type) {
        Name = name;
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class WeaveFunctionAttribute : Attribute {
    public string Path { get; }

    public WeaveFunctionAttribute(string path) => Path = path;
}

internal class WeaveType : WeaveLibraryEntry {
    public Type Type { get; }

    public WeaveType(Type type, string name) {
        Type = type;
        Name = name;
    }
}