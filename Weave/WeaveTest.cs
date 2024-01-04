using System.IO;
using System.Linq;

using Antlr4.Runtime.Tree;


namespace Weave {
    public static class WeaveTest {
        public static void StartTest() {
            const string root  = "../../../weave_scripts/";
            var          files = Directory.GetFiles(root).Select(path => new WeaveFileDefinition(path)).ToArray();

            var globalLibrary = new WeaveLibrary();

            // First pass to index libraries
            foreach (var f in files) {
                var firstPassListener = new WeaveLibraryListener(f, globalLibrary);
                ParseTreeWalker.Default.Walk(firstPassListener, f.Tree);
            }

            // Second parse to create expressions
            foreach (var f in files) {
                var secondPassListener = new WeaveListener(f, globalLibrary);
                ParseTreeWalker.Default.Walk(secondPassListener, f.Tree);
            }

            foreach (var f in files) {
                var instance   = f.CreateInstance();
                var startEvent = globalLibrary["builtin/start"] as WeaveEventInfo;
                instance.Invoke(startEvent!, 1, 3);
            }
        }
    }
}