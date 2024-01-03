using System;
using System.Linq;


namespace Weave;


public class WeaveLibraryListener : WeaveParserBaseListener {
    private readonly WeaveFileDefinition _file;
    private readonly WeaveLibrary        _globalLibrary;

    public WeaveLibraryListener(WeaveFileDefinition file, WeaveLibrary globalLibrary) {
        _file  = file;
        _globalLibrary = globalLibrary;
    }

    public override void EnterStart(WeaveParser.StartContext context) {
        foreach (var topLevelContext in context.topLevel()) {
            if (topLevelContext.GetChild(0) is not WeaveParser.EventContext eventContext) continue;

            var id         = eventContext.identifier().Start.Text;
            var parameters = eventContext.type().Select(t => WeaveListener.ParseType(t.Start.Type));
            _file.Library[id] = new WeaveEventInfo(id, parameters.ToArray());
        }

        foreach (var topLevelContext in context.topLevel()) {
            if (topLevelContext.GetChild(0) is not WeaveParser.ExportStatementContext exportContext) continue;

            var id        = exportContext.identifier().Start.Text;
            var globalKey = $"{_file.Name}/{id}";
            Console.WriteLine($"Exporting {globalKey}");
            _globalLibrary[globalKey] = _file.Library[id];
        }
    }
}