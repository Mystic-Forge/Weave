//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from WeaveLexer.g4 by ANTLR 4.13.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.1")]
[System.CLSCompliant(false)]
public partial class WeaveLexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		COMMENT=1, BLOCK_COMMENT=2, ON=3, IN=4, WITH=5, DO=6, TEMP=7, PRINT=8, 
		IF=9, THEN=10, ELSE=11, END=12, IMPORT=13, EXPORT=14, EVENT=15, BEING=16, 
		WHILE=17, FOR=18, EXIT=19, NEXT=20, MEMORY=21, SAVE=22, LOAD=23, AS=24, 
		FUNCTION=25, RETURN=26, OF=27, ASSIGN=28, PLUS=29, MINUS=30, MULTIPLY=31, 
		MOD=32, IS_NOT=33, IS=34, GREATER=35, LESS=36, GREATER_EQUAL=37, LESS_EQUAL=38, 
		AND=39, OR=40, NOT=41, SLASH=42, COMMA=43, LPAREN=44, RPAREN=45, LBRACKET=46, 
		RBRACKET=47, DOT=48, NIL=49, BOOL=50, INT=51, FLOAT=52, STRING=53, NAME=54, 
		WS=55;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"COMMENT", "BLOCK_COMMENT", "ON", "IN", "WITH", "DO", "TEMP", "PRINT", 
		"IF", "THEN", "ELSE", "END", "IMPORT", "EXPORT", "EVENT", "BEING", "WHILE", 
		"FOR", "EXIT", "NEXT", "MEMORY", "SAVE", "LOAD", "AS", "FUNCTION", "RETURN", 
		"OF", "ASSIGN", "PLUS", "MINUS", "MULTIPLY", "MOD", "IS_NOT", "IS", "GREATER", 
		"LESS", "GREATER_EQUAL", "LESS_EQUAL", "AND", "OR", "NOT", "SLASH", "COMMA", 
		"LPAREN", "RPAREN", "LBRACKET", "RBRACKET", "DOT", "NIL", "BOOL", "INT", 
		"FLOAT", "STRING", "NAME", "WS", "Digit"
	};


	public WeaveLexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public WeaveLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, null, null, "'on'", "'in'", "'with'", "'do'", "'temp'", "'print'", 
		"'if'", "'then'", "'else'", "'end'", "'import'", "'export'", "'event'", 
		"'being'", "'while'", "'for'", "'exit'", "'next'", "'memory'", "'save'", 
		"'load'", "'as'", "'function'", "'return'", "'of'", "'='", "'+'", "'-'", 
		"'*'", "'mod'", "'is not'", "'is'", "'>'", "'<'", "'>='", "'<='", "'and'", 
		"'or'", "'not'", "'/'", "','", "'('", "')'", "'['", "']'", "'.'", "'nil'"
	};
	private static readonly string[] _SymbolicNames = {
		null, "COMMENT", "BLOCK_COMMENT", "ON", "IN", "WITH", "DO", "TEMP", "PRINT", 
		"IF", "THEN", "ELSE", "END", "IMPORT", "EXPORT", "EVENT", "BEING", "WHILE", 
		"FOR", "EXIT", "NEXT", "MEMORY", "SAVE", "LOAD", "AS", "FUNCTION", "RETURN", 
		"OF", "ASSIGN", "PLUS", "MINUS", "MULTIPLY", "MOD", "IS_NOT", "IS", "GREATER", 
		"LESS", "GREATER_EQUAL", "LESS_EQUAL", "AND", "OR", "NOT", "SLASH", "COMMA", 
		"LPAREN", "RPAREN", "LBRACKET", "RBRACKET", "DOT", "NIL", "BOOL", "INT", 
		"FLOAT", "STRING", "NAME", "WS"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "WeaveLexer.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override int[] SerializedAtn { get { return _serializedATN; } }

	static WeaveLexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static int[] _serializedATN = {
		4,0,55,377,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,2,26,7,26,2,27,7,27,2,28,
		7,28,2,29,7,29,2,30,7,30,2,31,7,31,2,32,7,32,2,33,7,33,2,34,7,34,2,35,
		7,35,2,36,7,36,2,37,7,37,2,38,7,38,2,39,7,39,2,40,7,40,2,41,7,41,2,42,
		7,42,2,43,7,43,2,44,7,44,2,45,7,45,2,46,7,46,2,47,7,47,2,48,7,48,2,49,
		7,49,2,50,7,50,2,51,7,51,2,52,7,52,2,53,7,53,2,54,7,54,2,55,7,55,1,0,1,
		0,5,0,116,8,0,10,0,12,0,119,9,0,1,0,3,0,122,8,0,1,0,1,0,1,1,1,1,1,1,1,
		1,5,1,130,8,1,10,1,12,1,133,9,1,1,1,1,1,1,1,1,1,1,1,1,2,1,2,1,2,1,3,1,
		3,1,3,1,4,1,4,1,4,1,4,1,4,1,5,1,5,1,5,1,6,1,6,1,6,1,6,1,6,1,7,1,7,1,7,
		1,7,1,7,1,7,1,8,1,8,1,8,1,9,1,9,1,9,1,9,1,9,1,10,1,10,1,10,1,10,1,10,1,
		11,1,11,1,11,1,11,1,12,1,12,1,12,1,12,1,12,1,12,1,12,1,13,1,13,1,13,1,
		13,1,13,1,13,1,13,1,14,1,14,1,14,1,14,1,14,1,14,1,15,1,15,1,15,1,15,1,
		15,1,15,1,16,1,16,1,16,1,16,1,16,1,16,1,17,1,17,1,17,1,17,1,18,1,18,1,
		18,1,18,1,18,1,19,1,19,1,19,1,19,1,19,1,20,1,20,1,20,1,20,1,20,1,20,1,
		20,1,21,1,21,1,21,1,21,1,21,1,22,1,22,1,22,1,22,1,22,1,23,1,23,1,23,1,
		24,1,24,1,24,1,24,1,24,1,24,1,24,1,24,1,24,1,25,1,25,1,25,1,25,1,25,1,
		25,1,25,1,26,1,26,1,26,1,27,1,27,1,28,1,28,1,29,1,29,1,30,1,30,1,31,1,
		31,1,31,1,31,1,32,1,32,1,32,1,32,1,32,1,32,1,32,1,33,1,33,1,33,1,34,1,
		34,1,35,1,35,1,36,1,36,1,36,1,37,1,37,1,37,1,38,1,38,1,38,1,38,1,39,1,
		39,1,39,1,40,1,40,1,40,1,40,1,41,1,41,1,42,1,42,1,43,1,43,1,44,1,44,1,
		45,1,45,1,46,1,46,1,47,1,47,1,48,1,48,1,48,1,48,1,49,1,49,1,49,1,49,1,
		49,1,49,1,49,1,49,1,49,3,49,337,8,49,1,50,4,50,340,8,50,11,50,12,50,341,
		1,51,4,51,345,8,51,11,51,12,51,346,1,51,1,51,4,51,351,8,51,11,51,12,51,
		352,1,52,1,52,5,52,357,8,52,10,52,12,52,360,9,52,1,52,1,52,1,53,4,53,365,
		8,53,11,53,12,53,366,1,54,4,54,370,8,54,11,54,12,54,371,1,54,1,54,1,55,
		1,55,3,117,131,358,0,56,1,1,3,2,5,3,7,4,9,5,11,6,13,7,15,8,17,9,19,10,
		21,11,23,12,25,13,27,14,29,15,31,16,33,17,35,18,37,19,39,20,41,21,43,22,
		45,23,47,24,49,25,51,26,53,27,55,28,57,29,59,30,61,31,63,32,65,33,67,34,
		69,35,71,36,73,37,75,38,77,39,79,40,81,41,83,42,85,43,87,44,89,45,91,46,
		93,47,95,48,97,49,99,50,101,51,103,52,105,53,107,54,109,55,111,0,1,0,4,
		1,1,10,10,4,0,48,57,65,90,95,95,97,122,3,0,9,10,13,13,32,32,1,0,48,57,
		384,0,1,1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,
		0,0,0,0,13,1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,
		0,23,1,0,0,0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,
		1,0,0,0,0,35,1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,
		0,0,45,1,0,0,0,0,47,1,0,0,0,0,49,1,0,0,0,0,51,1,0,0,0,0,53,1,0,0,0,0,55,
		1,0,0,0,0,57,1,0,0,0,0,59,1,0,0,0,0,61,1,0,0,0,0,63,1,0,0,0,0,65,1,0,0,
		0,0,67,1,0,0,0,0,69,1,0,0,0,0,71,1,0,0,0,0,73,1,0,0,0,0,75,1,0,0,0,0,77,
		1,0,0,0,0,79,1,0,0,0,0,81,1,0,0,0,0,83,1,0,0,0,0,85,1,0,0,0,0,87,1,0,0,
		0,0,89,1,0,0,0,0,91,1,0,0,0,0,93,1,0,0,0,0,95,1,0,0,0,0,97,1,0,0,0,0,99,
		1,0,0,0,0,101,1,0,0,0,0,103,1,0,0,0,0,105,1,0,0,0,0,107,1,0,0,0,0,109,
		1,0,0,0,1,113,1,0,0,0,3,125,1,0,0,0,5,139,1,0,0,0,7,142,1,0,0,0,9,145,
		1,0,0,0,11,150,1,0,0,0,13,153,1,0,0,0,15,158,1,0,0,0,17,164,1,0,0,0,19,
		167,1,0,0,0,21,172,1,0,0,0,23,177,1,0,0,0,25,181,1,0,0,0,27,188,1,0,0,
		0,29,195,1,0,0,0,31,201,1,0,0,0,33,207,1,0,0,0,35,213,1,0,0,0,37,217,1,
		0,0,0,39,222,1,0,0,0,41,227,1,0,0,0,43,234,1,0,0,0,45,239,1,0,0,0,47,244,
		1,0,0,0,49,247,1,0,0,0,51,256,1,0,0,0,53,263,1,0,0,0,55,266,1,0,0,0,57,
		268,1,0,0,0,59,270,1,0,0,0,61,272,1,0,0,0,63,274,1,0,0,0,65,278,1,0,0,
		0,67,285,1,0,0,0,69,288,1,0,0,0,71,290,1,0,0,0,73,292,1,0,0,0,75,295,1,
		0,0,0,77,298,1,0,0,0,79,302,1,0,0,0,81,305,1,0,0,0,83,309,1,0,0,0,85,311,
		1,0,0,0,87,313,1,0,0,0,89,315,1,0,0,0,91,317,1,0,0,0,93,319,1,0,0,0,95,
		321,1,0,0,0,97,323,1,0,0,0,99,336,1,0,0,0,101,339,1,0,0,0,103,344,1,0,
		0,0,105,354,1,0,0,0,107,364,1,0,0,0,109,369,1,0,0,0,111,375,1,0,0,0,113,
		117,5,35,0,0,114,116,9,0,0,0,115,114,1,0,0,0,116,119,1,0,0,0,117,118,1,
		0,0,0,117,115,1,0,0,0,118,121,1,0,0,0,119,117,1,0,0,0,120,122,7,0,0,0,
		121,120,1,0,0,0,122,123,1,0,0,0,123,124,6,0,0,0,124,2,1,0,0,0,125,126,
		5,35,0,0,126,127,5,45,0,0,127,131,1,0,0,0,128,130,9,0,0,0,129,128,1,0,
		0,0,130,133,1,0,0,0,131,132,1,0,0,0,131,129,1,0,0,0,132,134,1,0,0,0,133,
		131,1,0,0,0,134,135,5,45,0,0,135,136,5,35,0,0,136,137,1,0,0,0,137,138,
		6,1,0,0,138,4,1,0,0,0,139,140,5,111,0,0,140,141,5,110,0,0,141,6,1,0,0,
		0,142,143,5,105,0,0,143,144,5,110,0,0,144,8,1,0,0,0,145,146,5,119,0,0,
		146,147,5,105,0,0,147,148,5,116,0,0,148,149,5,104,0,0,149,10,1,0,0,0,150,
		151,5,100,0,0,151,152,5,111,0,0,152,12,1,0,0,0,153,154,5,116,0,0,154,155,
		5,101,0,0,155,156,5,109,0,0,156,157,5,112,0,0,157,14,1,0,0,0,158,159,5,
		112,0,0,159,160,5,114,0,0,160,161,5,105,0,0,161,162,5,110,0,0,162,163,
		5,116,0,0,163,16,1,0,0,0,164,165,5,105,0,0,165,166,5,102,0,0,166,18,1,
		0,0,0,167,168,5,116,0,0,168,169,5,104,0,0,169,170,5,101,0,0,170,171,5,
		110,0,0,171,20,1,0,0,0,172,173,5,101,0,0,173,174,5,108,0,0,174,175,5,115,
		0,0,175,176,5,101,0,0,176,22,1,0,0,0,177,178,5,101,0,0,178,179,5,110,0,
		0,179,180,5,100,0,0,180,24,1,0,0,0,181,182,5,105,0,0,182,183,5,109,0,0,
		183,184,5,112,0,0,184,185,5,111,0,0,185,186,5,114,0,0,186,187,5,116,0,
		0,187,26,1,0,0,0,188,189,5,101,0,0,189,190,5,120,0,0,190,191,5,112,0,0,
		191,192,5,111,0,0,192,193,5,114,0,0,193,194,5,116,0,0,194,28,1,0,0,0,195,
		196,5,101,0,0,196,197,5,118,0,0,197,198,5,101,0,0,198,199,5,110,0,0,199,
		200,5,116,0,0,200,30,1,0,0,0,201,202,5,98,0,0,202,203,5,101,0,0,203,204,
		5,105,0,0,204,205,5,110,0,0,205,206,5,103,0,0,206,32,1,0,0,0,207,208,5,
		119,0,0,208,209,5,104,0,0,209,210,5,105,0,0,210,211,5,108,0,0,211,212,
		5,101,0,0,212,34,1,0,0,0,213,214,5,102,0,0,214,215,5,111,0,0,215,216,5,
		114,0,0,216,36,1,0,0,0,217,218,5,101,0,0,218,219,5,120,0,0,219,220,5,105,
		0,0,220,221,5,116,0,0,221,38,1,0,0,0,222,223,5,110,0,0,223,224,5,101,0,
		0,224,225,5,120,0,0,225,226,5,116,0,0,226,40,1,0,0,0,227,228,5,109,0,0,
		228,229,5,101,0,0,229,230,5,109,0,0,230,231,5,111,0,0,231,232,5,114,0,
		0,232,233,5,121,0,0,233,42,1,0,0,0,234,235,5,115,0,0,235,236,5,97,0,0,
		236,237,5,118,0,0,237,238,5,101,0,0,238,44,1,0,0,0,239,240,5,108,0,0,240,
		241,5,111,0,0,241,242,5,97,0,0,242,243,5,100,0,0,243,46,1,0,0,0,244,245,
		5,97,0,0,245,246,5,115,0,0,246,48,1,0,0,0,247,248,5,102,0,0,248,249,5,
		117,0,0,249,250,5,110,0,0,250,251,5,99,0,0,251,252,5,116,0,0,252,253,5,
		105,0,0,253,254,5,111,0,0,254,255,5,110,0,0,255,50,1,0,0,0,256,257,5,114,
		0,0,257,258,5,101,0,0,258,259,5,116,0,0,259,260,5,117,0,0,260,261,5,114,
		0,0,261,262,5,110,0,0,262,52,1,0,0,0,263,264,5,111,0,0,264,265,5,102,0,
		0,265,54,1,0,0,0,266,267,5,61,0,0,267,56,1,0,0,0,268,269,5,43,0,0,269,
		58,1,0,0,0,270,271,5,45,0,0,271,60,1,0,0,0,272,273,5,42,0,0,273,62,1,0,
		0,0,274,275,5,109,0,0,275,276,5,111,0,0,276,277,5,100,0,0,277,64,1,0,0,
		0,278,279,5,105,0,0,279,280,5,115,0,0,280,281,5,32,0,0,281,282,5,110,0,
		0,282,283,5,111,0,0,283,284,5,116,0,0,284,66,1,0,0,0,285,286,5,105,0,0,
		286,287,5,115,0,0,287,68,1,0,0,0,288,289,5,62,0,0,289,70,1,0,0,0,290,291,
		5,60,0,0,291,72,1,0,0,0,292,293,5,62,0,0,293,294,5,61,0,0,294,74,1,0,0,
		0,295,296,5,60,0,0,296,297,5,61,0,0,297,76,1,0,0,0,298,299,5,97,0,0,299,
		300,5,110,0,0,300,301,5,100,0,0,301,78,1,0,0,0,302,303,5,111,0,0,303,304,
		5,114,0,0,304,80,1,0,0,0,305,306,5,110,0,0,306,307,5,111,0,0,307,308,5,
		116,0,0,308,82,1,0,0,0,309,310,5,47,0,0,310,84,1,0,0,0,311,312,5,44,0,
		0,312,86,1,0,0,0,313,314,5,40,0,0,314,88,1,0,0,0,315,316,5,41,0,0,316,
		90,1,0,0,0,317,318,5,91,0,0,318,92,1,0,0,0,319,320,5,93,0,0,320,94,1,0,
		0,0,321,322,5,46,0,0,322,96,1,0,0,0,323,324,5,110,0,0,324,325,5,105,0,
		0,325,326,5,108,0,0,326,98,1,0,0,0,327,328,5,116,0,0,328,329,5,114,0,0,
		329,330,5,117,0,0,330,337,5,101,0,0,331,332,5,102,0,0,332,333,5,97,0,0,
		333,334,5,108,0,0,334,335,5,115,0,0,335,337,5,101,0,0,336,327,1,0,0,0,
		336,331,1,0,0,0,337,100,1,0,0,0,338,340,3,111,55,0,339,338,1,0,0,0,340,
		341,1,0,0,0,341,339,1,0,0,0,341,342,1,0,0,0,342,102,1,0,0,0,343,345,3,
		111,55,0,344,343,1,0,0,0,345,346,1,0,0,0,346,344,1,0,0,0,346,347,1,0,0,
		0,347,348,1,0,0,0,348,350,5,46,0,0,349,351,3,111,55,0,350,349,1,0,0,0,
		351,352,1,0,0,0,352,350,1,0,0,0,352,353,1,0,0,0,353,104,1,0,0,0,354,358,
		5,34,0,0,355,357,9,0,0,0,356,355,1,0,0,0,357,360,1,0,0,0,358,359,1,0,0,
		0,358,356,1,0,0,0,359,361,1,0,0,0,360,358,1,0,0,0,361,362,5,34,0,0,362,
		106,1,0,0,0,363,365,7,1,0,0,364,363,1,0,0,0,365,366,1,0,0,0,366,364,1,
		0,0,0,366,367,1,0,0,0,367,108,1,0,0,0,368,370,7,2,0,0,369,368,1,0,0,0,
		370,371,1,0,0,0,371,369,1,0,0,0,371,372,1,0,0,0,372,373,1,0,0,0,373,374,
		6,54,0,0,374,110,1,0,0,0,375,376,7,3,0,0,376,112,1,0,0,0,11,0,117,121,
		131,336,341,346,352,358,366,371,1,6,0,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
