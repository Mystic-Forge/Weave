using System;
using System.Linq;


namespace Weave;


public class WeaveLibraryListener : WeaveParserBaseListener {
    private readonly WeaveScriptDefinition _script;
    private readonly WeaveLibrary          _globalLibrary;

    public WeaveLibraryListener(WeaveScriptDefinition script, WeaveLibrary globalLibrary) {
        _script        = script;
        _globalLibrary = globalLibrary;
    }

    public override void EnterStart(WeaveParser.StartContext context) {
        foreach (var importContext in context.topLevel().Select(tl => tl.GetChild(0)).OfType<WeaveParser.ImportStatementContext>()) WeaveListener.DoImport(_script, _globalLibrary, importContext, t => t is WeaveType);

        foreach (var topLevelContext in context.topLevel()) {
            switch (topLevelContext.GetChild(0)) {
                case WeaveParser.Self_assertionContext selfAssertionContext: {
                    var id   = selfAssertionContext.identifier().Start.Text;
                    var type = WeaveListener.ParseType(id, _script.LocalLibrary);
                    _script.SelfType = type;
                    break;
                }
                case WeaveParser.EventContext eventContext: {
                    var id = eventContext.identifier().Start.Text;
                    var type = eventContext.type() is not null ? WeaveListener.ParseType(eventContext.type().Start.Text, _script.LocalLibrary) : null;

                    var parameters = eventContext.labeled_type()
                        .ToDictionary(lt => lt.identifier().First().Start.Text, lt => WeaveListener.ParseType(lt.identifier().Last().Start.Text, _script.LocalLibrary));

                    _script.LocalLibrary.Set(id, new WeaveEventInfo(id, type, parameters));
                    break;
                }
                case WeaveParser.MemoryContext memoryContext: {
                    var id   = memoryContext.identifier().First().Start.Text;
                    var type = WeaveListener.ParseType(memoryContext.identifier().Last().Start.Text, _script.LocalLibrary);
                    _script.LocalLibrary.Set(id, new WeaveMemoryInfo(id, type));
                    break;
                }
                case WeaveParser.FunctionContext functionContext: {
                    var id = functionContext.identifier().Last().Start.Text;

                    var parameters = functionContext.labeled_type()
                        .ToDictionary(lt => lt.identifier().First().Start.Text, lt => WeaveListener.ParseType(lt.identifier().Last().Start.Text, _script.LocalLibrary));

                    var returnType = functionContext.identifier().Length > 1 ? WeaveListener.ParseType(functionContext.identifier().First().Start.Text, _script.LocalLibrary) : null;

                    _script.LocalLibrary.Set(id, new WeaveFunctionInfo(_script, functionContext, id, parameters, returnType));
                    break;
                }
            }
        }
        
        foreach (var topLevelContext in context.topLevel()) {
            if (topLevelContext.GetChild(0) is not WeaveParser.ExportStatementContext exportContext) continue;

            var id          = exportContext.identifier().Start.Text;
            var sep         = _script.Library.LibraryPath == "" ? "" : "/";
            var globalKey   = $"{_script.Library.LibraryPath}{sep}{_script.Name}/{id}";
            var localExportEntry = _script.LocalLibrary.GetFirst(id);
            _globalLibrary.Set(globalKey, localExportEntry);
            // Console.WriteLine($"Global is local {_globalLibrary.GetFirst(globalKey) == localExportEntry}");
        }
    }
}