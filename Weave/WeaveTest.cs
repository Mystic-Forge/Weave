using System;
using System.IO;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


namespace Weave {
    public static class WeaveTest {
        public static void StartTest() {
            var input  = File.ReadAllText("../../../test.wv");
            var stream = new AntlrInputStream(input);
            var lexer  = new WeaveLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new WeaveParser(tokens);
            parser.BuildParseTree = true;
            var tree = parser.start();

            var method  = new WeaveEventInfo("start");
            var library = new WeaveLibrary();
            library["start"] = method;

            var listener = new WeaveListener(library);

            ParseTreeWalker.Default.Walk(listener, tree);

            listener.FileDefinition.CreateInstance().Invoke(method);
        }
    }
}